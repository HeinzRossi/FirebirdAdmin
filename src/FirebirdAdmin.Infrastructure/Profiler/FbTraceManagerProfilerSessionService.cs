using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using FirebirdAdmin.Application.Connections;
using FirebirdAdmin.Application.Profiler;
using FirebirdAdmin.Infrastructure.Security;

namespace FirebirdAdmin.Infrastructure.Profiler;

public sealed class FbTraceManagerProfilerSessionService(
    ITraceConfigurationBuilder traceConfigurationBuilder,
    ITraceEventParser traceEventParser,
    ITraceProcessRunner traceProcessRunner) : IProfilerSessionService
{
    private static readonly Regex TraceSessionStartedRegex = new(@"Trace session ID\s+(?<id>\d+)\s+started", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly Channel<ProfilerEvent> channel = Channel.CreateUnbounded<ProfilerEvent>(
        new UnboundedChannelOptions { SingleReader = false, SingleWriter = true });

    private readonly object sync = new();
    private CancellationTokenSource? sessionCts;
    private Task? runningTask;
    private long nextSequence = 1;
    private string? traceConfigPath;
    private string? passwordFetchPath;
    private int? activeTraceSessionId;
    private string? activeSessionName;
    private ToolsetCandidate? activeTraceManager;
    private ConnectionContext? activeConnection;

    public ProfilerState State { get; private set; } = ProfilerState.Disconnected;

    public Task<ProfilerSession> StartAsync(
        ProfilerOptions options,
        CredentialSecret? password,
        CancellationToken cancellationToken)
    {
        lock (sync)
        {
            if (State is ProfilerState.Running or ProfilerState.Starting)
            {
                throw new InvalidOperationException("Profiler já está em execução.");
            }

            State = ProfilerState.Starting;
        }

        var traceManager = options.Connection.Toolset.Candidates.FirstOrDefault(
            candidate => candidate.Kind == FirebirdToolKind.TraceManager && candidate.IsAvailable);

        if (traceManager is null || string.IsNullOrWhiteSpace(traceManager.Path))
        {
            State = ProfilerState.Failed;
            throw new InvalidOperationException("fbtracemgr não encontrado no toolset ativo.");
        }

        sessionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        passwordFetchPath = null;
        activeTraceManager = traceManager;
        activeConnection = options.Connection;
        activeSessionName = options.SessionName;
        activeTraceSessionId = null;

        var passwordBytes = password?.CopyBytes() ?? [];
        try
        {
            if (passwordBytes.Length > 0)
            {
                passwordFetchPath = Path.Combine(Path.GetTempPath(), $"firebird-admin-trace-pwd-{Guid.NewGuid():N}.tmp");
                WritePasswordFetchFile(passwordFetchPath, passwordBytes);
            }
        }
        finally
        {
            Array.Clear(passwordBytes);
        }

        State = ProfilerState.Running;
        runningTask = RunTraceAsync(options, traceManager, sessionCts.Token);

        return Task.FromResult(new ProfilerSession(
            Guid.NewGuid(),
            options.SessionName,
            DateTimeOffset.UtcNow,
            State));
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        State = ProfilerState.Stopping;

        var stopIssued = await TryStopActiveTraceSessionAsync(cancellationToken);

        if (!stopIssued && sessionCts is not null)
        {
            await sessionCts.CancelAsync();
        }

        if (runningTask is not null)
        {
            try
            {
                await runningTask.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
            }
            catch (TimeoutException)
            {
                if (sessionCts is not null)
                {
                    await sessionCts.CancelAsync();
                }
            }
        }

        CleanupTemporaryFiles();
        State = ProfilerState.Ready;
    }

    public IAsyncEnumerable<ProfilerEvent> ReadAllAsync(CancellationToken cancellationToken)
    {
        return channel.Reader.ReadAllAsync(cancellationToken);
    }

    private async Task RunTraceAsync(
        ProfilerOptions options,
        ToolsetCandidate traceManager,
        CancellationToken cancellationToken)
    {
        try
        {
            var initialDialect = ResolveInitialDialect(options.Connection, traceManager);
            var fallbackDialect = GetAlternateDialect(initialDialect);
            var firstAttempt = await RunTraceAttemptAsync(options, traceManager, initialDialect, cancellationToken);

            if (firstAttempt.ParseConfigurationFailed && !cancellationToken.IsCancellationRequested)
            {
                var secondAttempt = await RunTraceAttemptAsync(options, traceManager, fallbackDialect, cancellationToken);
                if (secondAttempt.ParseConfigurationFailed)
                {
                    State = ProfilerState.Failed;
                    await PublishTechnicalAsync(
                        BuildTraceConfigurationFailureMessage(options.Connection, traceManager, initialDialect, fallbackDialect, secondAttempt),
                        CancellationToken.None);
                    return;
                }

                if (secondAttempt.ExitCode != 0)
                {
                    State = ProfilerState.Failed;
                    await PublishTechnicalAsync($"fbtracemgr finalizou com código {secondAttempt.ExitCode}.", CancellationToken.None);
                }

                return;
            }

            if (firstAttempt.ExitCode != 0)
            {
                State = ProfilerState.Failed;
                await PublishTechnicalAsync($"fbtracemgr finalizou com código {firstAttempt.ExitCode}.", CancellationToken.None);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            State = ProfilerState.Failed;
            await PublishTechnicalAsync(SecretMasker.MaskSecrets(ex.Message), CancellationToken.None);
        }
        finally
        {
            CleanupTemporaryFiles();
        }
    }

    private async Task<TraceAttemptResult> RunTraceAttemptAsync(
        ProfilerOptions options,
        ToolsetCandidate traceManager,
        TraceConfigurationDialect dialect,
        CancellationToken cancellationToken)
    {
        var attemptConfigPath = Path.Combine(Path.GetTempPath(), $"firebird-admin-trace-{Guid.NewGuid():N}.conf");
        traceConfigPath = attemptConfigPath;

        File.WriteAllText(
            attemptConfigPath,
            traceConfigurationBuilder.Build(options, dialect),
            Encoding.ASCII);

        var request = new TraceProcessRequest(
            traceManager.Path,
            BuildArguments(options, attemptConfigPath),
            UseFileRedirection: true);

        var block = new StringBuilder();
        var bufferedOutput = new List<string>();
        var bufferedErrors = new List<string>();
        var bufferLock = new object();
        var parseErrorDetected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var accepted = false;
        var suppressFinalFlush = false;

        using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        async Task OnAttemptOutputLineAsync(string line, CancellationToken token)
        {
            CaptureTraceSessionId(line);

            if (IsTraceConfigurationParseError(line))
            {
                AddBufferedLine(bufferedOutput, line);
                parseErrorDetected.TrySetResult();
                return;
            }

            if (ShouldBuffer())
            {
                AddBufferedLine(bufferedOutput, line);
                await AcceptBufferedAttemptIfReadyAsync(token);
                return;
            }

            await OnOutputLineAsync(line, block, token);
        }

        async Task OnAttemptErrorLineAsync(string line, CancellationToken token)
        {
            if (IsTraceConfigurationParseError(line))
            {
                AddBufferedLine(bufferedErrors, line);
                parseErrorDetected.TrySetResult();
                return;
            }

            if (ShouldBuffer())
            {
                AddBufferedLine(bufferedErrors, line);
                return;
            }

            await PublishTechnicalAsync(line, token);
        }

        var runnerTask = traceProcessRunner.RunAsync(
            request,
            OnAttemptOutputLineAsync,
            OnAttemptErrorLineAsync,
            attemptCts.Token);

        try
        {
            var completed = await Task.WhenAny(runnerTask, parseErrorDetected.Task);

            if (completed == parseErrorDetected.Task)
            {
                suppressFinalFlush = true;
                if (!runnerTask.IsCompleted)
                {
                    await attemptCts.CancelAsync();
                    await WaitForAttemptCancellationAsync(runnerTask);
                }

                return new TraceAttemptResult(dialect, ExitCode: 1, ParseConfigurationFailed: true, bufferedOutput, bufferedErrors);
            }

            if (completed == runnerTask)
            {
                var exitCode = await runnerTask;
                var failedByParse = HasTraceConfigurationParseError(bufferedOutput) || HasTraceConfigurationParseError(bufferedErrors);
                if (!failedByParse && !WasAccepted())
                {
                    await AcceptBufferedAttemptAsync(cancellationToken);
                }
                else
                {
                    suppressFinalFlush = true;
                }

                return new TraceAttemptResult(dialect, exitCode, failedByParse, bufferedOutput, bufferedErrors);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            var failedByParse = HasTraceConfigurationParseError(bufferedOutput) || HasTraceConfigurationParseError(bufferedErrors);
            if (!failedByParse && !WasAccepted())
            {
                await AcceptBufferedAttemptAsync(CancellationToken.None);
            }

            throw;
        }
        finally
        {
            if (!suppressFinalFlush)
            {
                await FlushBlockAsync(block, CancellationToken.None);
            }

            DeleteIfExists(attemptConfigPath);
            if (string.Equals(traceConfigPath, attemptConfigPath, StringComparison.OrdinalIgnoreCase))
            {
                traceConfigPath = null;
            }
        }

        throw new InvalidOperationException("Tentativa de Trace finalizada sem resultado.");

        void AddBufferedLine(List<string> target, string line)
        {
            lock (bufferLock)
            {
                target.Add(line);
            }
        }

        bool ShouldBuffer()
        {
            lock (bufferLock)
            {
                return !accepted;
            }
        }

        bool WasAccepted()
        {
            lock (bufferLock)
            {
                return accepted;
            }
        }

        async Task AcceptBufferedAttemptIfReadyAsync(CancellationToken token)
        {
            lock (bufferLock)
            {
                if (accepted || !HasCompleteRecognizedTraceBlock(bufferedOutput))
                {
                    return;
                }

                accepted = true;
            }

            await AcceptBufferedAttemptAsync(token);
            await PublishTechnicalAsync($"Profiler Trace em execução com dialeto {dialect}.", token);
        }

        async Task AcceptBufferedAttemptAsync(CancellationToken token)
        {
            IReadOnlyList<string> outputSnapshot;
            IReadOnlyList<string> errorSnapshot;
            lock (bufferLock)
            {
                outputSnapshot = bufferedOutput.ToArray();
                errorSnapshot = bufferedErrors.ToArray();
            }

            await FlushBufferedLinesAsync(outputSnapshot, errorSnapshot, block, token);
        }
    }

    private async Task<bool> TryStopActiveTraceSessionAsync(CancellationToken cancellationToken)
    {
        if (activeTraceManager is null || activeConnection is null)
        {
            return false;
        }

        var traceSessionId = activeTraceSessionId ?? await ResolveActiveTraceSessionIdAsync(activeTraceManager, activeConnection, cancellationToken);
        if (traceSessionId is null)
        {
            return false;
        }

        using var stopCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        stopCts.CancelAfter(TimeSpan.FromSeconds(5));

        try
        {
            var request = new TraceProcessRequest(
                activeTraceManager.Path,
                BuildStopArguments(activeConnection, traceSessionId.Value));

            _ = await traceProcessRunner.RunAsync(
                request,
                static (_, _) => Task.CompletedTask,
                static (_, _) => Task.CompletedTask,
                stopCts.Token);

            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private async Task<int?> ResolveActiveTraceSessionIdAsync(
        ToolsetCandidate traceManager,
        ConnectionContext connection,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(activeSessionName))
        {
            return null;
        }

        var lines = new List<string>();
        using var listCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        listCts.CancelAfter(TimeSpan.FromSeconds(5));

        try
        {
            var request = new TraceProcessRequest(traceManager.Path, BuildListArguments(connection));
            _ = await traceProcessRunner.RunAsync(
                request,
                (line, _) =>
                {
                    lines.Add(line);
                    return Task.CompletedTask;
                },
                static (_, _) => Task.CompletedTask,
                listCts.Token);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }

        return FindTraceSessionIdByName(lines, activeSessionName);
    }

    private static int? FindTraceSessionIdByName(IReadOnlyList<string> lines, string sessionName)
    {
        int? candidateId = null;
        foreach (var line in lines)
        {
            var idMatch = Regex.Match(line, @"Session ID:\s*(?<id>\d+)", RegexOptions.IgnoreCase);
            if (idMatch.Success && int.TryParse(idMatch.Groups["id"].Value, out var parsedId))
            {
                candidateId = parsedId;
                continue;
            }

            var trimmed = line.Trim();
            if (candidateId is not null &&
                trimmed.StartsWith("name:", StringComparison.OrdinalIgnoreCase) &&
                trimmed[5..].Trim().Equals(sessionName, StringComparison.OrdinalIgnoreCase))
            {
                return candidateId;
            }
        }

        return null;
    }

    private void CaptureTraceSessionId(string line)
    {
        var match = TraceSessionStartedRegex.Match(line);
        if (!match.Success || !int.TryParse(match.Groups["id"].Value, out var parsedId))
        {
            return;
        }

        activeTraceSessionId = parsedId;
    }

    private static bool HasCompleteRecognizedTraceBlock(IReadOnlyList<string> lines)
    {
        if (lines.Count == 0 || !string.IsNullOrWhiteSpace(lines[^1]))
        {
            return false;
        }

        return lines.Any(line =>
            line.Contains("EXECUTE_STATEMENT", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("PREPARE_STATEMENT", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("statement finished", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("statement start", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("statement prepare", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task WaitForAttemptCancellationAsync(Task runnerTask)
    {
        try
        {
            await runnerTask;
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task FlushBufferedLinesAsync(
        IReadOnlyList<string> outputLines,
        IReadOnlyList<string> errorLines,
        StringBuilder block,
        CancellationToken cancellationToken)
    {
        foreach (var line in outputLines)
        {
            await OnOutputLineAsync(line, block, cancellationToken);
        }

        foreach (var line in errorLines)
        {
            await PublishTechnicalAsync(line, cancellationToken);
        }
    }

    private async Task OnOutputLineAsync(string line, StringBuilder block, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            if (IsFirebird25StatementHeaderWithoutSql(block))
            {
                block.AppendLine();
                return;
            }

            await FlushBlockAsync(block, cancellationToken);
            return;
        }

        block.AppendLine(line);
    }

    private static bool IsFirebird25StatementHeaderWithoutSql(StringBuilder block)
    {
        if (block.Length == 0)
        {
            return false;
        }

        var text = block.ToString();
        return text.Contains("EXECUTE_STATEMENT", StringComparison.OrdinalIgnoreCase) &&
               !text.Contains("Statement ", StringComparison.OrdinalIgnoreCase);
    }

    private async Task FlushBlockAsync(StringBuilder block, CancellationToken cancellationToken)
    {
        if (block.Length == 0)
        {
            return;
        }

        var text = block.ToString();
        block.Clear();

        var sequence = Interlocked.Read(ref nextSequence);
        foreach (var profilerEvent in traceEventParser.ParseBlock(text, sequence, DateTimeOffset.UtcNow))
        {
            Interlocked.Exchange(ref nextSequence, profilerEvent.Sequence + 1);
            await channel.Writer.WriteAsync(profilerEvent, cancellationToken);
        }
    }

    private async Task PublishTechnicalAsync(string line, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        var technicalLine = NormalizeTechnicalLine(line);
        var sequence = Interlocked.Increment(ref nextSequence);
        await channel.Writer.WriteAsync(
            new ProfilerEvent(
                sequence,
                DateTimeOffset.UtcNow,
                TraceEventType.Technical,
                null,
                null,
                null,
                null,
                null,
                new ProfilerMetrics(),
                null,
                MaskTechnicalLine(technicalLine)),
            cancellationToken);
    }

    private static string NormalizeTechnicalLine(string line)
    {
        return line.Contains("error while parsing trace configuration", StringComparison.OrdinalIgnoreCase)
            ? $"Configuração Trace incompatível com esta versão do Firebird: {line}"
            : line;
    }

    private static void WritePasswordFetchFile(string path, byte[] passwordBytes)
    {
        var newline = Encoding.UTF8.GetBytes(Environment.NewLine);
        using var stream = new FileStream(
            path,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 4096,
            FileOptions.WriteThrough);

        stream.Write(passwordBytes, 0, passwordBytes.Length);
        stream.Write(newline, 0, newline.Length);
        stream.Flush(flushToDisk: true);

        var info = new FileInfo(path);
        if (!info.Exists || info.Length == 0)
        {
            throw new IOException("Não foi possível preparar o arquivo temporário de senha do Trace.");
        }
    }

    private string MaskTechnicalLine(string line)
    {
        var masked = SecretMasker.MaskSecrets(line);
        if (!string.IsNullOrWhiteSpace(passwordFetchPath))
        {
            masked = masked.Replace(passwordFetchPath, "<arquivo-senha-trace>", StringComparison.OrdinalIgnoreCase);
        }

        return masked;
    }

    private IReadOnlyList<string> BuildArguments(ProfilerOptions options, string configPath)
    {
        var arguments = new List<string>
        {
            "-se",
            $"{options.Connection.Host}:service_mgr",
            "-start",
            "-name",
            options.SessionName,
            "-config",
            configPath,
            "-user",
            options.Connection.UserName
        };

        if (passwordFetchPath is not null)
        {
            arguments.Add("-fetch");
            arguments.Add(passwordFetchPath);
        }

        return arguments;
    }

    private IReadOnlyList<string> BuildStopArguments(ConnectionContext connection, int traceSessionId)
    {
        var arguments = new List<string>
        {
            "-se",
            $"{connection.Host}:service_mgr",
            "-stop",
            "-id",
            traceSessionId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "-user",
            connection.UserName
        };

        if (passwordFetchPath is not null)
        {
            arguments.Add("-fetch");
            arguments.Add(passwordFetchPath);
        }

        return arguments;
    }

    private IReadOnlyList<string> BuildListArguments(ConnectionContext connection)
    {
        var arguments = new List<string>
        {
            "-se",
            $"{connection.Host}:service_mgr",
            "-list",
            "-user",
            connection.UserName
        };

        if (passwordFetchPath is not null)
        {
            arguments.Add("-fetch");
            arguments.Add(passwordFetchPath);
        }

        return arguments;
    }

    private static TraceConfigurationDialect ResolveInitialDialect(
        ConnectionContext connection,
        ToolsetCandidate traceManager)
    {
        var toolVersion = FirebirdServerVersion.Parse(traceManager.Version);
        if (toolVersion.Major > 0)
        {
            return toolVersion.Major <= 2
                ? TraceConfigurationDialect.Legacy25
                : TraceConfigurationDialect.Modern30Plus;
        }

        return connection.ServerVersion.Major <= 2
            ? TraceConfigurationDialect.Legacy25
            : TraceConfigurationDialect.Modern30Plus;
    }

    private static TraceConfigurationDialect GetAlternateDialect(TraceConfigurationDialect dialect)
    {
        return dialect is TraceConfigurationDialect.Legacy25
            ? TraceConfigurationDialect.Modern30Plus
            : TraceConfigurationDialect.Legacy25;
    }

    private static bool HasTraceConfigurationParseError(IEnumerable<string> lines)
    {
        return lines.Any(IsTraceConfigurationParseError);
    }

    private static bool IsTraceConfigurationParseError(string line)
    {
        return line.Contains("error while parsing trace configuration", StringComparison.OrdinalIgnoreCase)
            || line.Contains("expected name, got", StringComparison.OrdinalIgnoreCase);
    }

    private string BuildTraceConfigurationFailureMessage(
        ConnectionContext connection,
        ToolsetCandidate traceManager,
        TraceConfigurationDialect firstDialect,
        TraceConfigurationDialect secondDialect,
        TraceAttemptResult failedAttempt)
    {
        var toolVersion = string.IsNullOrWhiteSpace(traceManager.Version) ? "desconhecida" : traceManager.Version;
        var detail = failedAttempt.ErrorLines.Concat(failedAttempt.OutputLines).FirstOrDefault(IsTraceConfigurationParseError)
            ?? "erro de parse não detalhado";

        return SecretMasker.MaskSecrets(
            $"Configuração Trace incompatível. Servidor Firebird {connection.ServerVersion.Raw}; " +
            $"fbtracemgr {toolVersion}; dialetos tentados: {firstDialect}, {secondDialect}. Detalhe: {detail}");
    }

    private void CleanupTemporaryFiles()
    {
        DeleteIfExists(traceConfigPath);
        DeleteIfExists(passwordFetchPath);
        traceConfigPath = null;
        passwordFetchPath = null;
        activeTraceSessionId = null;
        activeSessionName = null;
        activeTraceManager = null;
        activeConnection = null;
    }

    private static void DeleteIfExists(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private sealed record TraceAttemptResult(
        TraceConfigurationDialect Dialect,
        int ExitCode,
        bool ParseConfigurationFailed,
        IReadOnlyList<string> OutputLines,
        IReadOnlyList<string> ErrorLines);
}

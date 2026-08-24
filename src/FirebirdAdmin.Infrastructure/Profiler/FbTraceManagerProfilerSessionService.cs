using System.Text;
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
    private static readonly TimeSpan StartupProbeTimeout = TimeSpan.FromSeconds(1);

    private readonly Channel<ProfilerEvent> channel = Channel.CreateUnbounded<ProfilerEvent>(
        new UnboundedChannelOptions { SingleReader = false, SingleWriter = true });

    private readonly object sync = new();
    private CancellationTokenSource? sessionCts;
    private Task? runningTask;
    private long nextSequence = 1;
    private string? traceConfigPath;
    private string? passwordFetchPath;

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

        if (sessionCts is not null)
        {
            await sessionCts.CancelAsync();
        }

        if (runningTask is not null)
        {
            try
            {
                await runningTask.WaitAsync(TimeSpan.FromSeconds(3), cancellationToken);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
            }
            catch (TimeoutException)
            {
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
            Encoding.UTF8);

        var request = new TraceProcessRequest(
            traceManager.Path,
            BuildArguments(options, attemptConfigPath));

        var block = new StringBuilder();
        var bufferedOutput = new List<string>();
        var bufferedErrors = new List<string>();
        var bufferLock = new object();
        var parseErrorDetected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var bufferingStartup = true;

        using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        async Task OnAttemptOutputLineAsync(string line, CancellationToken token)
        {
            if (IsTraceConfigurationParseError(line))
            {
                parseErrorDetected.TrySetResult();
            }

            if (ShouldBuffer())
            {
                lock (bufferLock)
                {
                    bufferedOutput.Add(line);
                }

                return;
            }

            await OnOutputLineAsync(line, block, token);
        }

        async Task OnAttemptErrorLineAsync(string line, CancellationToken token)
        {
            if (IsTraceConfigurationParseError(line))
            {
                parseErrorDetected.TrySetResult();
            }

            if (ShouldBuffer())
            {
                lock (bufferLock)
                {
                    bufferedErrors.Add(line);
                }

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
            var startupDelay = Task.Delay(StartupProbeTimeout, cancellationToken);
            var completed = await Task.WhenAny(runnerTask, startupDelay, parseErrorDetected.Task);

            if (completed == parseErrorDetected.Task && !runnerTask.IsCompleted)
            {
                await attemptCts.CancelAsync();
                await WaitForAttemptCancellationAsync(runnerTask);
                return new TraceAttemptResult(dialect, ExitCode: 1, ParseConfigurationFailed: true, bufferedOutput, bufferedErrors);
            }

            if (completed == runnerTask)
            {
                var exitCode = await runnerTask;
                var failedByParse = HasTraceConfigurationParseError(bufferedOutput) || HasTraceConfigurationParseError(bufferedErrors);
                if (!failedByParse)
                {
                    await FlushBufferedLinesAsync(bufferedOutput, bufferedErrors, block, cancellationToken);
                }

                return new TraceAttemptResult(dialect, exitCode, failedByParse, bufferedOutput, bufferedErrors);
            }

            bufferingStartup = false;
            await FlushBufferedLinesAsync(bufferedOutput, bufferedErrors, block, cancellationToken);
            var runningExitCode = await runnerTask;
            return new TraceAttemptResult(dialect, runningExitCode, ParseConfigurationFailed: false, bufferedOutput, bufferedErrors);
        }
        finally
        {
            await FlushBlockAsync(block, CancellationToken.None);
            DeleteIfExists(attemptConfigPath);
            if (string.Equals(traceConfigPath, attemptConfigPath, StringComparison.OrdinalIgnoreCase))
            {
                traceConfigPath = null;
            }
        }

        bool ShouldBuffer()
        {
            lock (bufferLock)
            {
                return bufferingStartup;
            }
        }
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
            await FlushBlockAsync(block, cancellationToken);
            return;
        }

        block.AppendLine(line);
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

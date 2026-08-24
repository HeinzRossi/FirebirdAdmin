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
        traceConfigPath = Path.Combine(Path.GetTempPath(), $"firebird-admin-trace-{Guid.NewGuid():N}.conf");
        passwordFetchPath = Path.Combine(Path.GetTempPath(), $"firebird-admin-trace-pwd-{Guid.NewGuid():N}.tmp");

        File.WriteAllText(
            traceConfigPath,
            traceConfigurationBuilder.Build(options, options.Connection.ServerVersion),
            Encoding.UTF8);

        var passwordBytes = password?.CopyBytes() ?? [];
        try
        {
            File.WriteAllBytes(passwordFetchPath, passwordBytes);
        }
        finally
        {
            Array.Clear(passwordBytes);
        }

        var request = new TraceProcessRequest(
            traceManager.Path,
            [
                "-se",
                $"{options.Connection.Host}:service_mgr",
                "-start",
                "-name",
                options.SessionName,
                "-config",
                traceConfigPath,
                "-user",
                options.Connection.UserName,
                "-fetch",
                passwordFetchPath
            ]);

        runningTask = RunTraceAsync(request, sessionCts.Token);
        State = ProfilerState.Running;

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

    private async Task RunTraceAsync(TraceProcessRequest request, CancellationToken cancellationToken)
    {
        var block = new StringBuilder();

        try
        {
            await traceProcessRunner.RunAsync(
                request,
                async (line, token) => await OnOutputLineAsync(line, block, token),
                async (line, token) => await PublishTechnicalAsync(line, token),
                cancellationToken);
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
            await FlushBlockAsync(block, CancellationToken.None);
            CleanupTemporaryFiles();
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
                SecretMasker.MaskSecrets(line)),
            cancellationToken);
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
}

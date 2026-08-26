using System.Runtime.CompilerServices;
using System.Threading.Channels;
using FirebirdAdmin.Application.Connections;

namespace FirebirdAdmin.Application.Monitoring;

public sealed class MonitoringSessionService(IMonitoringQueryStrategy queryStrategy) : IMonitoringSessionService
{
    private readonly SemaphoreSlim sessionGate = new(1, 1);
    private Channel<MonitoringSnapshot>? snapshots;
    private CancellationTokenSource? pollingCts;
    private Task? pollingTask;

    public MonitoringSessionStatus Status { get; private set; } = new(PollingState.Stopped, "Polling parado.", DateTimeOffset.UtcNow);

    public async Task<MonitoringSession> StartAsync(
        ConnectionContext connection,
        ConnectionProfile profile,
        CredentialSecret? password,
        PollingOptions options,
        CancellationToken cancellationToken)
    {
        await sessionGate.WaitAsync(cancellationToken);
        try
        {
            await StopCoreAsync(cancellationToken);

            var session = new MonitoringSession(Guid.NewGuid(), connection, options, DateTimeOffset.UtcNow);
            var sessionPassword = password is null ? null : CredentialSecret.FromBytes(password.CopyBytes());
            snapshots = Channel.CreateBounded<MonitoringSnapshot>(
                new BoundedChannelOptions(8)
                {
                    SingleReader = true,
                    SingleWriter = true,
                    FullMode = BoundedChannelFullMode.DropOldest
                });
            pollingCts = new CancellationTokenSource();
            pollingTask = RunPollingAsync(session, profile, sessionPassword, snapshots.Writer, pollingCts.Token);
            return session;
        }
        finally
        {
            sessionGate.Release();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await sessionGate.WaitAsync(cancellationToken);
        try
        {
            await StopCoreAsync(cancellationToken);
        }
        finally
        {
            sessionGate.Release();
        }
    }

    public async IAsyncEnumerable<MonitoringSnapshot> ReadAllAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var sessionSnapshots = snapshots;
        if (sessionSnapshots is null)
        {
            yield break;
        }

        await foreach (var snapshot in sessionSnapshots.Reader.ReadAllAsync(cancellationToken))
        {
            yield return snapshot;
        }
    }

    private async Task StopCoreAsync(CancellationToken cancellationToken)
    {
        if (pollingCts is null)
        {
            Status = new MonitoringSessionStatus(PollingState.Stopped, "Polling parado.", DateTimeOffset.UtcNow);
            return;
        }

        await pollingCts.CancelAsync();
        if (pollingTask is not null)
        {
            try
            {
                await pollingTask.WaitAsync(TimeSpan.FromSeconds(3), cancellationToken);
            }
            catch (TimeoutException)
            {
                Status = new MonitoringSessionStatus(PollingState.Reconnecting, "O polling não respondeu ao cancelamento.", DateTimeOffset.UtcNow);
                throw new TimeoutException("Não foi possível encerrar a sessão de monitoramento em até 3 segundos.");
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
            }
        }

        pollingCts.Dispose();
        pollingCts = null;
        pollingTask = null;
        Status = new MonitoringSessionStatus(PollingState.Stopped, "Polling parado.", DateTimeOffset.UtcNow);
    }

    private async Task RunPollingAsync(
        MonitoringSession session,
        ConnectionProfile profile,
        CredentialSecret? password,
        ChannelWriter<MonitoringSnapshot> writer,
        CancellationToken cancellationToken)
    {
        var interval = session.Options.MinInterval;
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Status = new MonitoringSessionStatus(PollingState.Connected, "Polling ativo.", DateTimeOffset.UtcNow);
                    var snapshot = await queryStrategy.CaptureAsync(session.Id, profile, password, cancellationToken);
                    await writer.WriteAsync(snapshot, cancellationToken);
                    interval = session.Options.MinInterval;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Status = new MonitoringSessionStatus(PollingState.Reconnecting, ex.Message, DateTimeOffset.UtcNow);
                    interval = TimeSpan.FromMilliseconds(Math.Min(interval.TotalMilliseconds * 2, session.Options.MaxInterval.TotalMilliseconds));
                }

                await Task.Delay(interval, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            password?.Dispose();
            writer.TryComplete();
        }
    }
}

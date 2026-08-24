using System.Runtime.CompilerServices;
using System.Threading.Channels;
using FirebirdAdmin.Application.Connections;

namespace FirebirdAdmin.Application.Monitoring;

public sealed class MonitoringSessionService(IMonitoringQueryStrategy queryStrategy) : IMonitoringSessionService
{
    private readonly Channel<MonitoringSnapshot> snapshots = Channel.CreateUnbounded<MonitoringSnapshot>(
        new UnboundedChannelOptions { SingleReader = false, SingleWriter = true });

    private CancellationTokenSource? pollingCts;
    private Task? pollingTask;
    private CredentialSecret? sessionPassword;

    public MonitoringSessionStatus Status { get; private set; } = new(PollingState.Stopped, "Polling parado.", DateTimeOffset.UtcNow);

    public Task<MonitoringSession> StartAsync(
        ConnectionContext connection,
        ConnectionProfile profile,
        CredentialSecret? password,
        PollingOptions options,
        CancellationToken cancellationToken)
    {
        pollingCts?.Cancel();
        pollingCts?.Dispose();
        sessionPassword?.Dispose();
        sessionPassword = password is null ? null : CredentialSecret.FromBytes(password.CopyBytes());

        var session = new MonitoringSession(Guid.NewGuid(), connection, options, DateTimeOffset.UtcNow);
        pollingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        pollingTask = RunPollingAsync(session, profile, sessionPassword, pollingCts.Token);

        return Task.FromResult(session);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (pollingCts is null)
        {
            Status = new MonitoringSessionStatus(PollingState.Stopped, "Polling parado.", DateTimeOffset.UtcNow);
            return;
        }

        await pollingCts.CancelAsync();

        if (pollingTask is not null)
        {
            await Task.WhenAny(pollingTask, Task.Delay(TimeSpan.FromSeconds(3), cancellationToken));
        }

        Status = new MonitoringSessionStatus(PollingState.Stopped, "Polling parado.", DateTimeOffset.UtcNow);
        sessionPassword?.Dispose();
        sessionPassword = null;
    }

    public async IAsyncEnumerable<MonitoringSnapshot> ReadAllAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var snapshot in snapshots.Reader.ReadAllAsync(cancellationToken))
        {
            yield return snapshot;
        }
    }

    private async Task RunPollingAsync(MonitoringSession session, ConnectionProfile profile, CredentialSecret? password, CancellationToken cancellationToken)
    {
        var interval = session.Options.MinInterval;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                Status = new MonitoringSessionStatus(PollingState.Connected, "Polling ativo.", DateTimeOffset.UtcNow);
                var snapshot = await queryStrategy.CaptureAsync(session.Id, profile, password, cancellationToken);
                await snapshots.Writer.WriteAsync(snapshot, cancellationToken);
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
}

using FirebirdAdmin.Application.Connections;

namespace FirebirdAdmin.Application.Monitoring;

public interface IMonitoringSessionService : IMonitoringSnapshotStream
{
    MonitoringSessionStatus Status { get; }

    Task<MonitoringSession> StartAsync(
        ConnectionContext connection,
        ConnectionProfile profile,
        CredentialSecret? password,
        PollingOptions options,
        CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);
}

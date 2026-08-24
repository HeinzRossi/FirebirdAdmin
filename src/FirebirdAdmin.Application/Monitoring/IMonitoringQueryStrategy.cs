using FirebirdAdmin.Application.Connections;

namespace FirebirdAdmin.Application.Monitoring;

public interface IMonitoringQueryStrategy
{
    Task<MonitoringSnapshot> CaptureAsync(
        Guid sessionId,
        ConnectionProfile profile,
        CredentialSecret? password,
        CancellationToken cancellationToken);
}

namespace FirebirdAdmin.Application.Monitoring;

public interface IMonitoringSnapshotStream
{
    IAsyncEnumerable<MonitoringSnapshot> ReadAllAsync(CancellationToken cancellationToken);
}

using FirebirdAdmin.Application.Monitoring;
using FirebirdAdmin.Application.Profiler;

namespace FirebirdAdmin.Application.History;

public interface IHistoryWriter
{
    Task WriteProfilerEventsAsync(Guid? connectionProfileId, IReadOnlyList<ProfilerEvent> events, CancellationToken cancellationToken);
    Task WriteMonitoringSnapshotsAsync(Guid? connectionProfileId, IReadOnlyList<MonitoringSnapshot> snapshots, CancellationToken cancellationToken);
}

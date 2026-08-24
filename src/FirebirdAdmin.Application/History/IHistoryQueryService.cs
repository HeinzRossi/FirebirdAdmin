namespace FirebirdAdmin.Application.History;

public interface IHistoryQueryService
{
    Task<HistoryPage<TraceEventHistoryItem>> QueryTraceEventsAsync(HistoryQuery query, CancellationToken cancellationToken);
    Task<HistoryPage<MonitoringSnapshotHistoryItem>> QueryMonitoringSnapshotsAsync(HistoryQuery query, CancellationToken cancellationToken);
}

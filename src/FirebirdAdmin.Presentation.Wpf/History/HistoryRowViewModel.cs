using FirebirdAdmin.Application.History;

namespace FirebirdAdmin.Presentation.Wpf.History;

public sealed class HistoryRowViewModel
{
    public HistoryRowViewModel(TraceEventHistoryItem item)
    {
        Id = item.Id;
        Timestamp = item.Timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
        Kind = "Trace";
        Type = item.Type.ToString();
        UserName = item.UserName;
        AttachmentId = item.AttachmentId;
        TransactionId = item.TransactionId;
        Duration = item.Duration?.TotalMilliseconds.ToString("0.###") ?? "-";
        Summary = item.Sql ?? item.RawTrace;
        Details = item.RawTrace;
    }

    public HistoryRowViewModel(MonitoringSnapshotHistoryItem item)
    {
        Id = item.Id;
        Timestamp = item.CapturedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
        Kind = "Monitoring";
        Type = "Snapshot";
        Duration = "-";
        Summary = $"Attachments: {item.AttachmentCount} | Transactions: {item.TransactionCount} | Statements: {item.StatementCount}";
        Details = Summary;
    }

    public long Id { get; }
    public string Timestamp { get; }
    public string Kind { get; }
    public string Type { get; }
    public string? UserName { get; }
    public long? AttachmentId { get; }
    public long? TransactionId { get; }
    public string Duration { get; }
    public string Summary { get; }
    public string Details { get; }
}

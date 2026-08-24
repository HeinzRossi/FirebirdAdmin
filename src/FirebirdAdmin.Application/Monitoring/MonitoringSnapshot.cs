namespace FirebirdAdmin.Application.Monitoring;

public sealed record MonitoringSnapshot(
    Guid SessionId,
    DateTimeOffset CapturedAt,
    IReadOnlyList<AttachmentSnapshot> Attachments,
    IReadOnlyList<TransactionSnapshot> Transactions,
    IReadOnlyList<StatementSnapshot> Statements);

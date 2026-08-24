namespace FirebirdAdmin.Application.History;

public sealed record MonitoringSnapshotHistoryItem(
    long Id,
    Guid? ConnectionProfileId,
    DateTimeOffset CapturedAt,
    int AttachmentCount,
    int TransactionCount,
    int StatementCount);

namespace FirebirdAdmin.Infrastructure.Persistence.Entities;

public sealed class PerformanceSnapshotEntity
{
    public long Id { get; set; }
    public Guid? ConnectionProfileId { get; set; }
    public DateTimeOffset CapturedAt { get; set; }
    public int AttachmentCount { get; set; }
    public int TransactionCount { get; set; }
    public int StatementCount { get; set; }
}

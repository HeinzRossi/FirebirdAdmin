namespace FirebirdAdmin.Infrastructure.Persistence.Entities;

public sealed class MaintenanceOperationLogEntity
{
    public long Id { get; set; }
    public Guid OperationId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string Stream { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

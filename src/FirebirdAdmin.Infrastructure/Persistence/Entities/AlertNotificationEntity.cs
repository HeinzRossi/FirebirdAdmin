namespace FirebirdAdmin.Infrastructure.Persistence.Entities;

public sealed class AlertNotificationEntity
{
    public long Id { get; set; }
    public Guid AlertId { get; set; }
    public string Channel { get; set; } = string.Empty;
    public DateTimeOffset SentAt { get; set; }
    public string Message { get; set; } = string.Empty;
}

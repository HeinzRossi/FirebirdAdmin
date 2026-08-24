namespace FirebirdAdmin.Infrastructure.Persistence.Entities;

public sealed class MonitoringSessionEntity
{
    public Guid Id { get; set; }
    public Guid? ConnectionProfileId { get; set; }
    public string Kind { get; set; } = string.Empty;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? StoppedAt { get; set; }
    public bool IsProtected { get; set; }
}

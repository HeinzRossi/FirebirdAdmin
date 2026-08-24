namespace FirebirdAdmin.Infrastructure.Persistence.Entities;

public sealed class MaintenanceOperationEntity
{
    public Guid Id { get; set; }
    public Guid? ConnectionProfileId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string? Target { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public int ExitCode { get; set; }
    public string Message { get; set; } = string.Empty;
}

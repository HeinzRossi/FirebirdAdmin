namespace FirebirdAdmin.Infrastructure.Persistence.Entities;

public sealed class AlertEventEntity
{
    public Guid Id { get; set; }
    public string RuleId { get; set; } = string.Empty;
    public string CorrelationKey { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string? TargetDisplayName { get; set; }
    public DateTimeOffset FirstSeen { get; set; }
    public DateTimeOffset LastSeen { get; set; }
    public int Occurrences { get; set; }
    public string EvidenceJson { get; set; } = "[]";
    public string? AcknowledgementNote { get; set; }
}

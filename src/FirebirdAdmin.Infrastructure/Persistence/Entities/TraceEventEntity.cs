namespace FirebirdAdmin.Infrastructure.Persistence.Entities;

public sealed class TraceEventEntity
{
    public long Id { get; set; }
    public Guid? ConnectionProfileId { get; set; }
    public long Sequence { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string Type { get; set; } = string.Empty;
    public double? DurationMs { get; set; }
    public string? UserName { get; set; }
    public long? AttachmentId { get; set; }
    public long? TransactionId { get; set; }
    public string? Sql { get; set; }
    public long? Reads { get; set; }
    public long? Writes { get; set; }
    public long? Fetches { get; set; }
    public long? Marks { get; set; }
    public string? Plan { get; set; }
    public string RawTrace { get; set; } = string.Empty;
}

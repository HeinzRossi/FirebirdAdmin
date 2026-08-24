namespace FirebirdAdmin.Infrastructure.Persistence.Entities;

public sealed class StatementExecutionEntity
{
    public long Id { get; set; }
    public Guid? ConnectionProfileId { get; set; }
    public long TraceEventId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string? Sql { get; set; }
    public double? DurationMs { get; set; }
}

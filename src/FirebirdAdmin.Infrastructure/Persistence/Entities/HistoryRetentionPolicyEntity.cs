namespace FirebirdAdmin.Infrastructure.Persistence.Entities;

public sealed class HistoryRetentionPolicyEntity
{
    public int Id { get; set; }
    public int RetentionDays { get; set; }
    public long MaxDatabaseBytes { get; set; }
    public int BatchSize { get; set; }
}

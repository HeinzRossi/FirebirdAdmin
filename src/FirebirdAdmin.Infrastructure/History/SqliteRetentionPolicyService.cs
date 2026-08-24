using Dapper;
using FirebirdAdmin.Application.History;
using FirebirdAdmin.Infrastructure.Persistence;

namespace FirebirdAdmin.Infrastructure.History;

public sealed class SqliteRetentionPolicyService(
    SqliteConnectionFactory connectionFactory,
    ApplicationDataPaths paths) : IRetentionPolicyService
{
    public async Task<RetentionPolicy> GetPolicyAsync(CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        var policy = await connection.QuerySingleOrDefaultAsync<RetentionPolicyRow>(new CommandDefinition(
            "SELECT RetentionDays, MaxDatabaseBytes, BatchSize FROM HistoryRetentionPolicies WHERE Id = 1;",
            cancellationToken: cancellationToken));

        return policy is null
            ? new RetentionPolicy()
            : new RetentionPolicy(policy.RetentionDays, policy.MaxDatabaseBytes, policy.BatchSize);
    }

    public async Task ApplyRetentionAsync(CancellationToken cancellationToken)
    {
        var policy = await GetPolicyAsync(cancellationToken);
        var cutoff = DateTimeOffset.UtcNow.AddDays(-policy.RetentionDays);

        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            """
            DELETE FROM TraceEvents
            WHERE Id IN (SELECT Id FROM TraceEvents WHERE Timestamp < @Cutoff ORDER BY Timestamp LIMIT @BatchSize);
            DELETE FROM MonitoringSnapshots
            WHERE Id IN (SELECT Id FROM MonitoringSnapshots WHERE CapturedAt < @Cutoff ORDER BY CapturedAt LIMIT @BatchSize);
            DELETE FROM PerformanceSnapshots
            WHERE Id IN (SELECT Id FROM PerformanceSnapshots WHERE CapturedAt < @Cutoff ORDER BY CapturedAt LIMIT @BatchSize);
            """,
            new { Cutoff = cutoff, policy.BatchSize },
            cancellationToken: cancellationToken));

        if (File.Exists(paths.DatabasePath) && new FileInfo(paths.DatabasePath).Length > policy.MaxDatabaseBytes)
        {
            await connection.ExecuteAsync(new CommandDefinition("VACUUM;", cancellationToken: cancellationToken));
        }
    }

    private sealed class RetentionPolicyRow
    {
        public int RetentionDays { get; set; }
        public long MaxDatabaseBytes { get; set; }
        public int BatchSize { get; set; }
    }
}

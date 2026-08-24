using Dapper;
using FirebirdAdmin.Application.History;
using FirebirdAdmin.Application.Monitoring;
using FirebirdAdmin.Application.Profiler;

namespace FirebirdAdmin.Infrastructure.History;

public sealed class DapperHistoryWriter(SqliteConnectionFactory connectionFactory) : IHistoryWriter
{
    public async Task WriteProfilerEventsAsync(Guid? connectionProfileId, IReadOnlyList<ProfilerEvent> events, CancellationToken cancellationToken)
    {
        if (events.Count == 0)
        {
            return;
        }

        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        foreach (var batch in events.Chunk(250))
        {
            await connection.ExecuteAsync(new CommandDefinition(
                """
                INSERT INTO TraceEvents (
                    ConnectionProfileId, Sequence, Timestamp, Type, DurationMs, UserName, AttachmentId, TransactionId,
                    Sql, Reads, Writes, Fetches, Marks, Plan, RawTrace)
                VALUES (
                    @ConnectionProfileId, @Sequence, @Timestamp, @Type, @DurationMs, @UserName, @AttachmentId, @TransactionId,
                    @Sql, @Reads, @Writes, @Fetches, @Marks, @Plan, @RawTrace);
                """,
                batch.Select(profilerEvent => new
                {
                    ConnectionProfileId = connectionProfileId,
                    profilerEvent.Sequence,
                    profilerEvent.Timestamp,
                    Type = profilerEvent.Type.ToString(),
                    DurationMs = profilerEvent.Duration?.TotalMilliseconds,
                    profilerEvent.UserName,
                    profilerEvent.AttachmentId,
                    profilerEvent.TransactionId,
                    profilerEvent.Sql,
                    profilerEvent.Metrics.Reads,
                    profilerEvent.Metrics.Writes,
                    profilerEvent.Metrics.Fetches,
                    profilerEvent.Metrics.Marks,
                    profilerEvent.Plan,
                    profilerEvent.RawTrace
                }),
                transaction,
                cancellationToken: cancellationToken));
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task WriteMonitoringSnapshotsAsync(Guid? connectionProfileId, IReadOnlyList<MonitoringSnapshot> snapshots, CancellationToken cancellationToken)
    {
        if (snapshots.Count == 0)
        {
            return;
        }

        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        foreach (var batch in snapshots.Chunk(250))
        {
            await connection.ExecuteAsync(new CommandDefinition(
                """
                INSERT INTO MonitoringSnapshots (
                    ConnectionProfileId, SessionId, CapturedAt, AttachmentCount, TransactionCount, StatementCount)
                VALUES (
                    @ConnectionProfileId, @SessionId, @CapturedAt, @AttachmentCount, @TransactionCount, @StatementCount);

                INSERT INTO PerformanceSnapshots (
                    ConnectionProfileId, CapturedAt, AttachmentCount, TransactionCount, StatementCount)
                VALUES (
                    @ConnectionProfileId, @CapturedAt, @AttachmentCount, @TransactionCount, @StatementCount);
                """,
                batch.Select(snapshot => new
                {
                    ConnectionProfileId = connectionProfileId,
                    snapshot.SessionId,
                    snapshot.CapturedAt,
                    AttachmentCount = snapshot.Attachments.Count,
                    TransactionCount = snapshot.Transactions.Count,
                    StatementCount = snapshot.Statements.Count
                }),
                transaction,
                cancellationToken: cancellationToken));
        }

        await transaction.CommitAsync(cancellationToken);
    }
}

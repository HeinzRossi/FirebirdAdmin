using System.Text;
using Dapper;
using FirebirdAdmin.Application.History;
using FirebirdAdmin.Application.Profiler;

namespace FirebirdAdmin.Infrastructure.History;

public sealed class DapperHistoryQueryService(SqliteConnectionFactory connectionFactory) : IHistoryQueryService
{
    public async Task<HistoryPage<TraceEventHistoryItem>> QueryTraceEventsAsync(HistoryQuery query, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        var (where, parameters) = BuildTraceWhere(query);
        var offset = (Math.Max(query.Page, 1) - 1) * Math.Clamp(query.PageSize, 1, 500);
        var pageSize = Math.Clamp(query.PageSize, 1, 500);
        parameters.Add("Offset", offset);
        parameters.Add("PageSize", pageSize);

        var total = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            $"SELECT COUNT(*) FROM TraceEvents {where};",
            parameters,
            cancellationToken: cancellationToken));

        var rows = await connection.QueryAsync<TraceEventRow>(new CommandDefinition(
            $"""
            SELECT Id, ConnectionProfileId, Sequence, Timestamp, Type, DurationMs, UserName, AttachmentId, TransactionId, Sql, Plan, RawTrace
            FROM TraceEvents
            {where}
            ORDER BY Timestamp DESC, Id DESC
            LIMIT @PageSize OFFSET @Offset;
            """,
            parameters,
            cancellationToken: cancellationToken));

        return new HistoryPage<TraceEventHistoryItem>(
            rows.Select(row => row.ToItem()).ToArray(),
            Math.Max(query.Page, 1),
            pageSize,
            total);
    }

    public async Task<HistoryPage<MonitoringSnapshotHistoryItem>> QueryMonitoringSnapshotsAsync(HistoryQuery query, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        var (where, parameters) = BuildMonitoringWhere(query);
        var offset = (Math.Max(query.Page, 1) - 1) * Math.Clamp(query.PageSize, 1, 500);
        var pageSize = Math.Clamp(query.PageSize, 1, 500);
        parameters.Add("Offset", offset);
        parameters.Add("PageSize", pageSize);

        var total = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            $"SELECT COUNT(*) FROM MonitoringSnapshots {where};",
            parameters,
            cancellationToken: cancellationToken));

        var rows = await connection.QueryAsync<MonitoringSnapshotRow>(new CommandDefinition(
            $"""
            SELECT Id, ConnectionProfileId, CapturedAt, AttachmentCount, TransactionCount, StatementCount
            FROM MonitoringSnapshots
            {where}
            ORDER BY CapturedAt DESC, Id DESC
            LIMIT @PageSize OFFSET @Offset;
            """,
            parameters,
            cancellationToken: cancellationToken));

        return new HistoryPage<MonitoringSnapshotHistoryItem>(
            rows.Select(row => row.ToItem()).ToArray(),
            Math.Max(query.Page, 1),
            pageSize,
            total);
    }

    private static (string Where, DynamicParameters Parameters) BuildTraceWhere(HistoryQuery query)
    {
        var clauses = new List<string>();
        var parameters = new DynamicParameters();
        AddCommon(query, clauses, parameters, "Timestamp");

        if (!string.IsNullOrWhiteSpace(query.SqlText))
        {
            clauses.Add("Sql LIKE @SqlText");
            parameters.Add("SqlText", $"%{query.SqlText}%");
        }

        if (query.TraceType is not null)
        {
            clauses.Add("Type = @TraceType");
            parameters.Add("TraceType", query.TraceType.ToString());
        }

        if (!string.IsNullOrWhiteSpace(query.UserName))
        {
            clauses.Add("UserName = @UserName");
            parameters.Add("UserName", query.UserName);
        }

        if (query.AttachmentId is not null)
        {
            clauses.Add("AttachmentId = @AttachmentId");
            parameters.Add("AttachmentId", query.AttachmentId);
        }

        if (query.TransactionId is not null)
        {
            clauses.Add("TransactionId = @TransactionId");
            parameters.Add("TransactionId", query.TransactionId);
        }

        if (query.MinimumDuration is not null)
        {
            clauses.Add("DurationMs >= @MinimumDurationMs");
            parameters.Add("MinimumDurationMs", query.MinimumDuration.Value.TotalMilliseconds);
        }

        return (ToWhere(clauses), parameters);
    }

    private static (string Where, DynamicParameters Parameters) BuildMonitoringWhere(HistoryQuery query)
    {
        var clauses = new List<string>();
        var parameters = new DynamicParameters();
        AddCommon(query, clauses, parameters, "CapturedAt");
        return (ToWhere(clauses), parameters);
    }

    private static void AddCommon(HistoryQuery query, List<string> clauses, DynamicParameters parameters, string timestampColumn)
    {
        if (query.From is not null)
        {
            clauses.Add($"{timestampColumn} >= @From");
            parameters.Add("From", query.From);
        }

        if (query.To is not null)
        {
            clauses.Add($"{timestampColumn} <= @To");
            parameters.Add("To", query.To);
        }

        if (query.ConnectionProfileId is not null)
        {
            clauses.Add("ConnectionProfileId = @ConnectionProfileId");
            parameters.Add("ConnectionProfileId", query.ConnectionProfileId);
        }
    }

    private static string ToWhere(IReadOnlyList<string> clauses)
    {
        return clauses.Count == 0 ? string.Empty : $"WHERE {string.Join(" AND ", clauses)}";
    }

    private sealed class TraceEventRow
    {
        public long Id { get; set; }
        public string? ConnectionProfileId { get; set; }
        public long Sequence { get; set; }
        public string Timestamp { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public double? DurationMs { get; set; }
        public string? UserName { get; set; }
        public long? AttachmentId { get; set; }
        public long? TransactionId { get; set; }
        public string? Sql { get; set; }
        public string? Plan { get; set; }
        public string RawTrace { get; set; } = string.Empty;

        public TraceEventHistoryItem ToItem()
        {
            return new TraceEventHistoryItem(
                Id,
                ParseGuid(ConnectionProfileId),
                Sequence,
                ParseTimestamp(Timestamp),
                Enum.TryParse<TraceEventType>(Type, out var type) ? type : TraceEventType.Unparsed,
                DurationMs is null ? null : TimeSpan.FromMilliseconds(DurationMs.Value),
                UserName,
                AttachmentId,
                TransactionId,
                Sql,
                Plan,
                RawTrace);
        }
    }

    private sealed class MonitoringSnapshotRow
    {
        public long Id { get; set; }
        public string? ConnectionProfileId { get; set; }
        public string CapturedAt { get; set; } = string.Empty;
        public int AttachmentCount { get; set; }
        public int TransactionCount { get; set; }
        public int StatementCount { get; set; }

        public MonitoringSnapshotHistoryItem ToItem()
        {
            return new MonitoringSnapshotHistoryItem(
                Id,
                ParseGuid(ConnectionProfileId),
                ParseTimestamp(CapturedAt),
                AttachmentCount,
                TransactionCount,
                StatementCount);
        }
    }

    private static Guid? ParseGuid(string? value)
    {
        return Guid.TryParse(value, out var parsed) ? parsed : null;
    }

    private static DateTimeOffset ParseTimestamp(string value)
    {
        return DateTimeOffset.TryParse(value, out var parsed) ? parsed : DateTimeOffset.MinValue;
    }
}

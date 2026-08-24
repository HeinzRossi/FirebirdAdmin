using Dapper;
using FirebirdAdmin.Application.Maintenance;
using FirebirdAdmin.Infrastructure.History;
using FirebirdAdmin.Infrastructure.Security;
using System.Globalization;

namespace FirebirdAdmin.Infrastructure.Maintenance;

public sealed class SqliteMaintenanceHistoryStore(SqliteConnectionFactory connectionFactory) : IMaintenanceHistoryStore
{
    public async Task SaveOperationAsync(MaintenanceOperation operation, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO MaintenanceOperations (
                Id, ConnectionProfileId, Type, Status, Source, Target, StartedAt, FinishedAt, ExitCode, Message)
            VALUES (
                @Id, @ConnectionProfileId, @Type, @Status, @Source, @Target, @StartedAt, @FinishedAt, @ExitCode, @Message)
            ON CONFLICT(Id) DO UPDATE SET
                Status = excluded.Status,
                FinishedAt = excluded.FinishedAt,
                ExitCode = excluded.ExitCode,
                Message = excluded.Message;
            """,
            new
            {
                operation.Id,
                operation.ConnectionProfileId,
                Type = operation.Type.ToString(),
                Status = operation.Status.ToString(),
                Source = SecretMasker.MaskSecrets(operation.Source),
                Target = SecretMasker.MaskSecrets(operation.Target),
                StartedAt = operation.StartedAt.ToString("O", CultureInfo.InvariantCulture),
                FinishedAt = operation.FinishedAt?.ToString("O", CultureInfo.InvariantCulture),
                operation.ExitCode,
                Message = SecretMasker.MaskSecrets(operation.Message)
            },
            cancellationToken: cancellationToken));
    }

    public async Task SaveLogAsync(MaintenanceLogLine logLine, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO MaintenanceOperationLogs (OperationId, Timestamp, Stream, Text)
            VALUES (@OperationId, @Timestamp, @Stream, @Text);
            """,
            new
            {
                logLine.OperationId,
                logLine.Timestamp,
                logLine.Stream,
                Text = SecretMasker.MaskSecrets(logLine.Text)
            },
            cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<MaintenanceOperation>> ListRecentAsync(int take, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        var rows = await connection.QueryAsync<MaintenanceOperationRow>(new CommandDefinition(
            """
            SELECT Id, ConnectionProfileId, Type, Status, Source, Target, StartedAt, FinishedAt, ExitCode, Message
            FROM MaintenanceOperations
            ORDER BY StartedAt DESC
            LIMIT @Take;
            """,
            new { Take = Math.Max(take, 1) },
            cancellationToken: cancellationToken));

        return rows.Select(row => new MaintenanceOperation(
            Guid.Parse(row.Id),
            string.IsNullOrWhiteSpace(row.ConnectionProfileId) ? null : Guid.Parse(row.ConnectionProfileId),
            Enum.Parse<MaintenanceOperationType>(row.Type),
            Enum.Parse<MaintenanceOperationStatus>(row.Status),
            row.Source,
            row.Target,
            DateTimeOffset.Parse(row.StartedAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            string.IsNullOrWhiteSpace(row.FinishedAt) ? null : DateTimeOffset.Parse(row.FinishedAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            row.ExitCode,
            row.Message)).ToArray();
    }

    private sealed class MaintenanceOperationRow
    {
        public string Id { get; set; } = string.Empty;
        public string? ConnectionProfileId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string? Target { get; set; }
        public string StartedAt { get; set; } = string.Empty;
        public string? FinishedAt { get; set; }
        public int ExitCode { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}

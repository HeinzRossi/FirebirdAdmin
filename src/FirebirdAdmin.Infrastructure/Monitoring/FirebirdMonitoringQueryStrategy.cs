using FirebirdAdmin.Application.Connections;
using FirebirdAdmin.Application.Monitoring;
using FirebirdSql.Data.FirebirdClient;

namespace FirebirdAdmin.Infrastructure.Monitoring;

public sealed class FirebirdMonitoringQueryStrategy : IMonitoringQueryStrategy
{
    public async Task<MonitoringSnapshot> CaptureAsync(
        Guid sessionId,
        ConnectionProfile profile,
        CredentialSecret? password,
        CancellationToken cancellationToken)
    {
        var connectionString = BuildConnectionString(profile, password);

        await using var connection = new FbConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var capturedAt = DateTimeOffset.UtcNow;
        var attachments = await ReadAttachmentsAsync(connection, cancellationToken);
        var transactions = await ReadTransactionsAsync(connection, cancellationToken);
        var statements = await ReadStatementsAsync(connection, cancellationToken);

        return new MonitoringSnapshot(sessionId, capturedAt, attachments, transactions, statements);
    }

    private static string BuildConnectionString(ConnectionProfile profile, CredentialSecret? password)
    {
        var builder = new FbConnectionStringBuilder
        {
            DataSource = profile.Host,
            Port = profile.Port,
            Database = profile.Database,
            UserID = profile.UserName,
            Password = password?.RevealAsString() ?? string.Empty,
            Pooling = false
        };

        if (!string.IsNullOrWhiteSpace(profile.Charset))
        {
            builder.Charset = profile.Charset;
        }

        if (!string.IsNullOrWhiteSpace(profile.Role))
        {
            builder.Role = profile.Role;
        }

        return builder.ToString();
    }

    private static async Task<IReadOnlyList<AttachmentSnapshot>> ReadAttachmentsAsync(FbConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            select
                mon$attachment_id,
                mon$user,
                mon$remote_address,
                mon$remote_process,
                mon$timestamp,
                mon$state
            from mon$attachments
            order by mon$attachment_id
            """;

        var rows = new List<AttachmentSnapshot>();
        await using var command = new FbCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new AttachmentSnapshot(
                GetInt64(reader, 0)!.Value,
                GetString(reader, 1),
                GetString(reader, 2),
                GetString(reader, 3),
                GetDateTimeOffset(reader, 4),
                GetNullableInt64(reader, 5)?.ToString()));
        }

        return rows;
    }

    private static async Task<IReadOnlyList<TransactionSnapshot>> ReadTransactionsAsync(FbConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            select
                mon$transaction_id,
                mon$attachment_id,
                mon$state,
                mon$timestamp,
                mon$top_transaction,
                mon$oldest_active,
                mon$isolation_mode,
                mon$lock_timeout
            from mon$transactions
            order by mon$transaction_id
            """;

        var rows = new List<TransactionSnapshot>();
        await using var command = new FbCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new TransactionSnapshot(
                GetInt64(reader, 0)!.Value,
                GetNullableInt64(reader, 1),
                GetNullableInt64(reader, 2)?.ToString(),
                GetDateTimeOffset(reader, 3),
                GetNullableInt64(reader, 4),
                GetNullableInt64(reader, 5),
                GetNullableInt64(reader, 6),
                GetNullableInt64(reader, 7)));
        }

        return rows;
    }

    private static async Task<IReadOnlyList<StatementSnapshot>> ReadStatementsAsync(FbConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            select
                mon$statement_id,
                mon$attachment_id,
                mon$transaction_id,
                mon$state,
                mon$timestamp,
                mon$sql_text
            from mon$statements
            order by mon$statement_id
            """;

        var rows = new List<StatementSnapshot>();
        await using var command = new FbCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new StatementSnapshot(
                GetInt64(reader, 0)!.Value,
                GetNullableInt64(reader, 1),
                GetNullableInt64(reader, 2),
                GetNullableInt64(reader, 3)?.ToString(),
                GetDateTimeOffset(reader, 4),
                GetString(reader, 5)));
        }

        return rows;
    }

    private static long? GetInt64(FbDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : Convert.ToInt64(reader.GetValue(ordinal), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static long? GetNullableInt64(FbDataReader reader, int ordinal)
    {
        return GetInt64(reader, ordinal);
    }

    private static string? GetString(FbDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static DateTimeOffset? GetDateTimeOffset(FbDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : new DateTimeOffset(reader.GetDateTime(ordinal));
    }
}

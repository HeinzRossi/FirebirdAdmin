using System.Text.Json;
using Dapper;
using FirebirdAdmin.Application.Diagnostics;
using FirebirdAdmin.Infrastructure.History;
using FirebirdAdmin.Infrastructure.Security;

namespace FirebirdAdmin.Infrastructure.Diagnostics;

public sealed class SqliteAlertStore(
    SqliteConnectionFactory connectionFactory,
    IAlertCorrelator correlator) : IAlertStore
{
    public async Task<Alert> UpsertAsync(DiagnosticResult result, CancellationToken cancellationToken)
    {
        var key = AlertCorrelator.BuildCorrelationKey(result);
        var existing = await GetByCorrelationKeyAsync(key, cancellationToken);
        var alert = Mask(correlator.Correlate(result, existing));

        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO AlertEvents (
                Id, RuleId, CorrelationKey, Severity, Status, Message, TargetType, TargetId, TargetDisplayName,
                FirstSeen, LastSeen, Occurrences, EvidenceJson, AcknowledgementNote)
            VALUES (
                @Id, @RuleId, @CorrelationKey, @Severity, @Status, @Message, @TargetType, @TargetId, @TargetDisplayName,
                @FirstSeen, @LastSeen, @Occurrences, @EvidenceJson, @AcknowledgementNote)
            ON CONFLICT(CorrelationKey) DO UPDATE SET
                Severity = excluded.Severity,
                Status = excluded.Status,
                Message = excluded.Message,
                TargetDisplayName = excluded.TargetDisplayName,
                LastSeen = excluded.LastSeen,
                Occurrences = excluded.Occurrences,
                EvidenceJson = excluded.EvidenceJson,
                AcknowledgementNote = excluded.AcknowledgementNote;
            """,
            ToRow(alert),
            cancellationToken: cancellationToken));

        return alert;
    }

    public async Task<IReadOnlyList<Alert>> ListAsync(AlertStatus? status, DiagnosticSeverity? severity, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        var clauses = new List<string>();
        var parameters = new DynamicParameters();
        if (status is not null)
        {
            clauses.Add("Status = @Status");
            parameters.Add("Status", status.ToString());
        }

        if (severity is not null)
        {
            clauses.Add("Severity = @Severity");
            parameters.Add("Severity", severity.ToString());
        }

        var where = clauses.Count == 0 ? string.Empty : $"WHERE {string.Join(" AND ", clauses)}";
        var rows = await connection.QueryAsync<AlertRow>(new CommandDefinition(
            $"""
            SELECT * FROM AlertEvents
            {where}
            ORDER BY LastSeen DESC
            LIMIT 500;
            """,
            parameters,
            cancellationToken: cancellationToken));

        return rows.Select(row => row.ToAlert()).ToArray();
    }

    public async Task<Alert?> GetByCorrelationKeyAsync(string correlationKey, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<AlertRow>(new CommandDefinition(
            "SELECT * FROM AlertEvents WHERE CorrelationKey = @CorrelationKey;",
            new { CorrelationKey = correlationKey },
            cancellationToken: cancellationToken));

        return row?.ToAlert();
    }

    public async Task SetStatusAsync(Guid id, AlertStatus status, string? note, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            """
            UPDATE AlertEvents
            SET Status = @Status, AcknowledgementNote = @Note
            WHERE Id = @Id;
            """,
            new { Id = id, Status = status.ToString(), Note = SecretMasker.MaskSecrets(note ?? string.Empty) },
            cancellationToken: cancellationToken));
    }

    private static object ToRow(Alert alert)
    {
        return new
        {
            alert.Id,
            alert.RuleId,
            alert.CorrelationKey,
            Severity = alert.Severity.ToString(),
            Status = alert.Status.ToString(),
            alert.Message,
            TargetType = alert.Target.Type,
            TargetId = alert.Target.Id,
            TargetDisplayName = alert.Target.DisplayName,
            alert.FirstSeen,
            alert.LastSeen,
            alert.Occurrences,
            EvidenceJson = JsonSerializer.Serialize(alert.Evidence),
            alert.AcknowledgementNote
        };
    }

    private static Alert Mask(Alert alert)
    {
        return alert with
        {
            Message = SecretMasker.MaskSecrets(alert.Message),
            Target = alert.Target with { DisplayName = SecretMasker.MaskSecrets(alert.Target.DisplayName ?? string.Empty) },
            Evidence = alert.Evidence
                .Select(evidence => evidence with { Value = evidence.Value is null ? null : SecretMasker.MaskSecrets(evidence.Value.ToString() ?? string.Empty) })
                .ToArray(),
            AcknowledgementNote = alert.AcknowledgementNote is null ? null : SecretMasker.MaskSecrets(alert.AcknowledgementNote)
        };
    }

    private sealed class AlertRow
    {
        public string Id { get; set; } = string.Empty;
        public string RuleId { get; set; } = string.Empty;
        public string CorrelationKey { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string TargetType { get; set; } = string.Empty;
        public string TargetId { get; set; } = string.Empty;
        public string? TargetDisplayName { get; set; }
        public string FirstSeen { get; set; } = string.Empty;
        public string LastSeen { get; set; } = string.Empty;
        public int Occurrences { get; set; }
        public string EvidenceJson { get; set; } = "[]";
        public string? AcknowledgementNote { get; set; }

        public Alert ToAlert()
        {
            return new Alert(
                Guid.TryParse(Id, out var id) ? id : Guid.Empty,
                RuleId,
                CorrelationKey,
                Enum.TryParse<DiagnosticSeverity>(Severity, out var severity) ? severity : DiagnosticSeverity.Info,
                Enum.TryParse<AlertStatus>(Status, out var status) ? status : AlertStatus.Active,
                Message,
                new DiagnosticTarget(TargetType, TargetId, TargetDisplayName),
                DateTimeOffset.TryParse(FirstSeen, out var firstSeen) ? firstSeen : DateTimeOffset.MinValue,
                DateTimeOffset.TryParse(LastSeen, out var lastSeen) ? lastSeen : DateTimeOffset.MinValue,
                Occurrences,
                JsonSerializer.Deserialize<DiagnosticEvidence[]>(EvidenceJson) ?? [],
                AcknowledgementNote);
        }
    }
}

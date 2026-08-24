namespace FirebirdAdmin.Application.Diagnostics.Rules;

public sealed class StaleSnapshotRule : IDiagnosticRule
{
    public string RuleId => "MON_STALE_OR_INCOMPLETE_SNAPSHOT";

    public IReadOnlyList<DiagnosticResult> Evaluate(DiagnosticContext context, DiagnosticRuleOptions options)
    {
        var snapshot = context.MonitoringSnapshot;
        if (snapshot is null)
        {
            return [];
        }

        var now = context.Now ?? DateTimeOffset.UtcNow;
        var stale = now - snapshot.CapturedAt > options.EffectiveStaleSnapshotThreshold;
        var incomplete = snapshot.Transactions.Any(transaction => transaction.StartedAt is null);

        if (!stale && !incomplete)
        {
            return [];
        }

        return
        [
            new DiagnosticResult(
                RuleId,
                stale ? DiagnosticSeverity.High : DiagnosticSeverity.Low,
                stale ? "Snapshot MON$ desatualizado." : "Snapshot MON$ com dados incompletos.",
                new DiagnosticTarget("MonitoringSession", snapshot.SessionId.ToString("N"), "Sessão MON$"),
                now,
                context.ConnectionProfileId,
                snapshot.SessionId,
                [
                    new("CapturedAt", snapshot.CapturedAt),
                    new("AgeSeconds", (long)(now - snapshot.CapturedAt).TotalSeconds, "s"),
                    new("HasIncompleteTransactions", incomplete)
                ])
        ];
    }
}

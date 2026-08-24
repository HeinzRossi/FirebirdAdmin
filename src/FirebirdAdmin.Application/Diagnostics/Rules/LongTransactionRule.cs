namespace FirebirdAdmin.Application.Diagnostics.Rules;

public sealed class LongTransactionRule : IDiagnosticRule
{
    public string RuleId => "MON_LONG_TRANSACTION";

    public IReadOnlyList<DiagnosticResult> Evaluate(DiagnosticContext context, DiagnosticRuleOptions options)
    {
        var snapshot = context.MonitoringSnapshot;
        if (snapshot is null)
        {
            return [];
        }

        var now = context.Now ?? DateTimeOffset.UtcNow;
        return snapshot.Transactions
            .Where(transaction => transaction.StartedAt is not null && now - transaction.StartedAt >= options.EffectiveLongTransactionThreshold)
            .Select(transaction => new DiagnosticResult(
                RuleId,
                DiagnosticSeverity.High,
                "Transação longa detectada.",
                new DiagnosticTarget("Transaction", transaction.TransactionId.ToString(), $"Transaction {transaction.TransactionId}"),
                now,
                context.ConnectionProfileId,
                snapshot.SessionId,
                [
                    new("TransactionId", transaction.TransactionId),
                    new("AgeSeconds", (long)(now - transaction.StartedAt!.Value).TotalSeconds, "s"),
                    new("ThresholdSeconds", (long)options.EffectiveLongTransactionThreshold.TotalSeconds, "s")
                ]))
            .ToArray();
    }
}

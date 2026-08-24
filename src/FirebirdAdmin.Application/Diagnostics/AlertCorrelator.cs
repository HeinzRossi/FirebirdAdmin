namespace FirebirdAdmin.Application.Diagnostics;

public sealed class AlertCorrelator : IAlertCorrelator
{
    public Alert Correlate(DiagnosticResult result, Alert? existing)
    {
        var key = BuildCorrelationKey(result);
        if (existing is null)
        {
            return new Alert(
                Guid.NewGuid(),
                result.RuleId,
                key,
                result.Severity,
                AlertStatus.Active,
                result.Message,
                result.Target,
                result.ObservedAt,
                result.ObservedAt,
                1,
                result.Evidence);
        }

        return existing with
        {
            Severity = result.Severity,
            Status = existing.Status is AlertStatus.Resolved ? AlertStatus.Active : existing.Status,
            Message = result.Message,
            LastSeen = result.ObservedAt,
            Occurrences = existing.Occurrences + 1,
            Evidence = result.Evidence
        };
    }

    public static string BuildCorrelationKey(DiagnosticResult result)
    {
        var scope = result.ConnectionProfileId?.ToString("N") ?? result.SessionId?.ToString("N") ?? "global";
        return $"{result.RuleId}|{scope}|{result.Target.Type}|{result.Target.Id}";
    }
}

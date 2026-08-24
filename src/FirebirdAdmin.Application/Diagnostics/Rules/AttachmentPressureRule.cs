namespace FirebirdAdmin.Application.Diagnostics.Rules;

public sealed class AttachmentPressureRule : IDiagnosticRule
{
    public string RuleId => "MON_ATTACHMENT_PRESSURE";

    public IReadOnlyList<DiagnosticResult> Evaluate(DiagnosticContext context, DiagnosticRuleOptions options)
    {
        var snapshot = context.MonitoringSnapshot;
        if (snapshot is null || snapshot.Attachments.Count < options.EffectiveAttachmentWarningThreshold)
        {
            return [];
        }

        var now = context.Now ?? DateTimeOffset.UtcNow;
        return
        [
            new DiagnosticResult(
                RuleId,
                DiagnosticSeverity.Medium,
                "Quantidade elevada de attachments.",
                new DiagnosticTarget("Database", snapshot.SessionId.ToString("N"), "Banco monitorado"),
                now,
                context.ConnectionProfileId,
                snapshot.SessionId,
                [
                    new("AttachmentCount", snapshot.Attachments.Count),
                    new("Threshold", options.EffectiveAttachmentWarningThreshold)
                ])
        ];
    }
}

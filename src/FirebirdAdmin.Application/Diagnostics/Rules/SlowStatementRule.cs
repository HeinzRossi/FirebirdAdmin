using FirebirdAdmin.Application.Profiler;

namespace FirebirdAdmin.Application.Diagnostics.Rules;

public sealed class SlowStatementRule : IDiagnosticRule
{
    public string RuleId => "TRACE_SLOW_STATEMENT";

    public IReadOnlyList<DiagnosticResult> Evaluate(DiagnosticContext context, DiagnosticRuleOptions options)
    {
        var profilerEvent = context.ProfilerEvent;
        if (profilerEvent is null ||
            profilerEvent.Type is not TraceEventType.StatementFinished ||
            profilerEvent.Duration is null ||
            profilerEvent.Duration < options.EffectiveSlowStatementThreshold)
        {
            return [];
        }

        return
        [
            new DiagnosticResult(
                RuleId,
                DiagnosticSeverity.Medium,
                "Statement SQL lento detectado.",
                new DiagnosticTarget("Statement", profilerEvent.Sequence.ToString(), $"Statement {profilerEvent.Sequence}"),
                profilerEvent.Timestamp,
                context.ConnectionProfileId,
                null,
                [
                    new("Sequence", profilerEvent.Sequence),
                    new("DurationMs", profilerEvent.Duration.Value.TotalMilliseconds, "ms"),
                    new("ThresholdMs", options.EffectiveSlowStatementThreshold.TotalMilliseconds, "ms"),
                    new("Sql", profilerEvent.Sql)
                ])
        ];
    }
}

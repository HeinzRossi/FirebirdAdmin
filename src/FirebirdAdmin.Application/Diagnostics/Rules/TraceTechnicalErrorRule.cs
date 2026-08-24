using FirebirdAdmin.Application.Profiler;

namespace FirebirdAdmin.Application.Diagnostics.Rules;

public sealed class TraceTechnicalErrorRule : IDiagnosticRule
{
    public string RuleId => "TRACE_TECHNICAL_EVENT";

    public IReadOnlyList<DiagnosticResult> Evaluate(DiagnosticContext context, DiagnosticRuleOptions options)
    {
        var profilerEvent = context.ProfilerEvent;
        if (profilerEvent is null || profilerEvent.Type is not (TraceEventType.Unparsed or TraceEventType.Technical))
        {
            return [];
        }

        return
        [
            new DiagnosticResult(
                RuleId,
                DiagnosticSeverity.Low,
                "Evento técnico ou não normalizado no Trace.",
                new DiagnosticTarget("Trace", profilerEvent.Sequence.ToString(), $"Trace {profilerEvent.Sequence}"),
                profilerEvent.Timestamp,
                context.ConnectionProfileId,
                null,
                [
                    new("Sequence", profilerEvent.Sequence),
                    new("Type", profilerEvent.Type.ToString()),
                    new("RawTrace", profilerEvent.RawTrace)
                ])
        ];
    }
}

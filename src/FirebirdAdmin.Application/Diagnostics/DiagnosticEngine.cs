using FirebirdAdmin.Application.Monitoring;
using FirebirdAdmin.Application.Profiler;

namespace FirebirdAdmin.Application.Diagnostics;

public sealed class DiagnosticEngine(IEnumerable<IDiagnosticRule> rules) : IDiagnosticEngine
{
    public IReadOnlyList<DiagnosticResult> Evaluate(MonitoringSnapshot snapshot, Guid? connectionProfileId, DiagnosticRuleOptions? options = null)
    {
        var context = new DiagnosticContext(connectionProfileId, MonitoringSnapshot: snapshot, Now: DateTimeOffset.UtcNow);
        return Evaluate(context, options);
    }

    public IReadOnlyList<DiagnosticResult> Evaluate(ProfilerEvent profilerEvent, Guid? connectionProfileId, Guid? sessionId = null, DiagnosticRuleOptions? options = null)
    {
        var context = new DiagnosticContext(connectionProfileId, ProfilerEvent: profilerEvent, Now: DateTimeOffset.UtcNow);
        return Evaluate(context, options, sessionId);
    }

    private IReadOnlyList<DiagnosticResult> Evaluate(DiagnosticContext context, DiagnosticRuleOptions? options, Guid? overrideSessionId = null)
    {
        var effectiveOptions = options ?? DiagnosticRuleOptions.Normal;
        return rules
            .SelectMany(rule => rule.Evaluate(context, effectiveOptions))
            .Select(result => result with { SessionId = result.SessionId ?? overrideSessionId })
            .ToArray();
    }
}

using FirebirdAdmin.Application.Monitoring;
using FirebirdAdmin.Application.Profiler;

namespace FirebirdAdmin.Application.Diagnostics;

public interface IDiagnosticEngine
{
    IReadOnlyList<DiagnosticResult> Evaluate(MonitoringSnapshot snapshot, Guid? connectionProfileId, DiagnosticRuleOptions? options = null);
    IReadOnlyList<DiagnosticResult> Evaluate(ProfilerEvent profilerEvent, Guid? connectionProfileId, Guid? sessionId = null, DiagnosticRuleOptions? options = null);
}

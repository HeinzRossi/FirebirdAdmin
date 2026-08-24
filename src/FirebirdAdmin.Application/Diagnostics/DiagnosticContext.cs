using FirebirdAdmin.Application.Monitoring;
using FirebirdAdmin.Application.Profiler;

namespace FirebirdAdmin.Application.Diagnostics;

public sealed record DiagnosticContext(
    Guid? ConnectionProfileId,
    MonitoringSnapshot? MonitoringSnapshot = null,
    ProfilerEvent? ProfilerEvent = null,
    DateTimeOffset? Now = null);

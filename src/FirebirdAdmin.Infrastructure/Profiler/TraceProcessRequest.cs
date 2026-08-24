namespace FirebirdAdmin.Infrastructure.Profiler;

public sealed record TraceProcessRequest(
    string ExecutablePath,
    IReadOnlyList<string> Arguments);

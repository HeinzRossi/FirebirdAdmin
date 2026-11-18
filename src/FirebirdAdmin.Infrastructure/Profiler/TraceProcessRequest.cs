namespace FirebirdAdmin.Infrastructure.Profiler;

public sealed record TraceProcessRequest(
    string ExecutablePath,
    IReadOnlyList<string> Arguments,
    IReadOnlyDictionary<string, string>? EnvironmentVariables = null,
    bool UseFileRedirection = false);

using FirebirdAdmin.Application.Connections;

namespace FirebirdAdmin.Application.Maintenance;

public sealed record FirebirdToolCommand(
    FirebirdToolKind ToolKind,
    string ExecutablePath,
    IReadOnlyList<string> Arguments,
    string WorkingDirectory,
    IReadOnlyDictionary<string, string> EnvironmentVariables);

public sealed record ToolExecutionResult(
    int ExitCode,
    IReadOnlyList<MaintenanceLogLine> Logs);

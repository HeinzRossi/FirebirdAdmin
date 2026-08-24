namespace FirebirdAdmin.Application.Diagnostics;

public sealed record DiagnosticEvidence(
    string Key,
    object? Value,
    string? Unit = null);

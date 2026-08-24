namespace FirebirdAdmin.Application.Diagnostics;

public sealed record DiagnosticTarget(
    string Type,
    string Id,
    string? DisplayName = null);

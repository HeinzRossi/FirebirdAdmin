namespace FirebirdAdmin.Application.Diagnostics;

public sealed record DiagnosticResult(
    string RuleId,
    DiagnosticSeverity Severity,
    string Message,
    DiagnosticTarget Target,
    DateTimeOffset ObservedAt,
    Guid? ConnectionProfileId,
    Guid? SessionId,
    IReadOnlyList<DiagnosticEvidence> Evidence);

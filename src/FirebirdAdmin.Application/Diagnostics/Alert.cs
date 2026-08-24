namespace FirebirdAdmin.Application.Diagnostics;

public sealed record Alert(
    Guid Id,
    string RuleId,
    string CorrelationKey,
    DiagnosticSeverity Severity,
    AlertStatus Status,
    string Message,
    DiagnosticTarget Target,
    DateTimeOffset FirstSeen,
    DateTimeOffset LastSeen,
    int Occurrences,
    IReadOnlyList<DiagnosticEvidence> Evidence,
    string? AcknowledgementNote = null);

namespace FirebirdAdmin.Application.Monitoring;

public sealed record StatementSnapshot(
    long StatementId,
    long? AttachmentId,
    long? TransactionId,
    string? State,
    DateTimeOffset? StartedAt,
    string? SqlText);

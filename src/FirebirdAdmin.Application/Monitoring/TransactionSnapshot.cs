namespace FirebirdAdmin.Application.Monitoring;

public sealed record TransactionSnapshot(
    long TransactionId,
    long? AttachmentId,
    string? State,
    DateTimeOffset? StartedAt,
    long? OldestTransaction,
    long? OldestActive,
    long? IsolationMode,
    long? LockTimeout);

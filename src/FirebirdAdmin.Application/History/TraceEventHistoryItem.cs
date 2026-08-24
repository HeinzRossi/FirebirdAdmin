using FirebirdAdmin.Application.Profiler;

namespace FirebirdAdmin.Application.History;

public sealed record TraceEventHistoryItem(
    long Id,
    Guid? ConnectionProfileId,
    long Sequence,
    DateTimeOffset Timestamp,
    TraceEventType Type,
    TimeSpan? Duration,
    string? UserName,
    long? AttachmentId,
    long? TransactionId,
    string? Sql,
    string? Plan,
    string RawTrace);

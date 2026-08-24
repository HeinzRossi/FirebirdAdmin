using FirebirdAdmin.Application.Profiler;

namespace FirebirdAdmin.Application.History;

public sealed record HistoryQuery(
    HistoryDataKind Kind = HistoryDataKind.TraceEvents,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    Guid? ConnectionProfileId = null,
    string? SqlText = null,
    TraceEventType? TraceType = null,
    string? UserName = null,
    long? AttachmentId = null,
    long? TransactionId = null,
    TimeSpan? MinimumDuration = null,
    int Page = 1,
    int PageSize = 100);

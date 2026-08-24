namespace FirebirdAdmin.Application.Profiler;

public sealed record ProfilerEvent(
    long Sequence,
    DateTimeOffset Timestamp,
    TraceEventType Type,
    TimeSpan? Duration,
    string? UserName,
    long? AttachmentId,
    long? TransactionId,
    string? Sql,
    ProfilerMetrics Metrics,
    string? Plan = null,
    string RawTrace = "");

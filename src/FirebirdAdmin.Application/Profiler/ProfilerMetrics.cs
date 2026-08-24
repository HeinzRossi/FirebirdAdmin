namespace FirebirdAdmin.Application.Profiler;

public sealed record ProfilerMetrics(
    long? Reads = null,
    long? Writes = null,
    long? Fetches = null,
    long? Marks = null);

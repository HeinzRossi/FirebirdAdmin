using FirebirdAdmin.Application.Connections;

namespace FirebirdAdmin.Application.Profiler;

public sealed record ProfilerOptions(
    ConnectionContext Connection,
    string SessionName,
    TimeSpan? SlowQueryThreshold = null,
    int MaxBufferedEvents = ProfilerBuffer.DefaultMaxEvents,
    TimeSpan? MaxBufferedAge = null);

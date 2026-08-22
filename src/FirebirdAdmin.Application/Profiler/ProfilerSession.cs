namespace FirebirdAdmin.Application.Profiler;

public sealed record ProfilerSession(
    Guid Id,
    string Name,
    DateTimeOffset StartedAt,
    ProfilerState State);

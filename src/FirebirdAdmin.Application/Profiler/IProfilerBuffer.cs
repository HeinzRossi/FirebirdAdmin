namespace FirebirdAdmin.Application.Profiler;

public interface IProfilerBuffer
{
    IReadOnlyList<ProfilerEvent> Events { get; }

    void Add(ProfilerEvent profilerEvent);
    void Clear();
    IReadOnlyList<ProfilerEvent> ApplyFilter(ProfilerFilter filter);
}

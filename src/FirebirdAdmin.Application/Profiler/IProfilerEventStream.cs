namespace FirebirdAdmin.Application.Profiler;

public interface IProfilerEventStream
{
    IAsyncEnumerable<ProfilerEvent> ReadAllAsync(CancellationToken cancellationToken);
}

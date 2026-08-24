namespace FirebirdAdmin.Infrastructure.Profiler;

public interface ITraceProcessRunner
{
    Task<int> RunAsync(
        TraceProcessRequest request,
        Func<string, CancellationToken, Task> onOutputLine,
        Func<string, CancellationToken, Task> onErrorLine,
        CancellationToken cancellationToken);
}

using FirebirdAdmin.Application.Connections;

namespace FirebirdAdmin.Application.Profiler;

public interface IProfilerSessionService
{
    ProfilerState State { get; }

    Task<ProfilerSession> StartAsync(
        ProfilerOptions options,
        CredentialSecret? password,
        CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);

    IAsyncEnumerable<ProfilerEvent> ReadAllAsync(CancellationToken cancellationToken);
}

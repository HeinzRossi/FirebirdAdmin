using FirebirdAdmin.Application.Connections;

namespace FirebirdAdmin.Application.Profiler;

public interface ITraceConfigurationBuilder
{
    string Build(ProfilerOptions options, FirebirdServerVersion serverVersion);
}

using Microsoft.Extensions.DependencyInjection;
using FirebirdAdmin.Application.Dashboard;
using FirebirdAdmin.Application.Monitoring;
using FirebirdAdmin.Application.Profiler;

namespace FirebirdAdmin.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IMonitoringSessionService, MonitoringSessionService>();
        services.AddSingleton<IDashboardProjectionService, DashboardProjectionService>();
        services.AddSingleton<ITraceEventParser, FirebirdTraceEventParser>();
        return services;
    }
}

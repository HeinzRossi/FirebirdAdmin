using Microsoft.Extensions.DependencyInjection;
using FirebirdAdmin.Application.Monitoring;

namespace FirebirdAdmin.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IMonitoringSessionService, MonitoringSessionService>();
        return services;
    }
}

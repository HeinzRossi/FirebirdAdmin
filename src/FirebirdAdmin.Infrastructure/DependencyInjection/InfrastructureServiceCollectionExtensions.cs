using FirebirdAdmin.Application.Connections;
using FirebirdAdmin.Application.Monitoring;
using FirebirdAdmin.Infrastructure.Connections;
using FirebirdAdmin.Infrastructure.Monitoring;
using FirebirdAdmin.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FirebirdAdmin.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ApplicationDataPaths>();
        services.AddDbContextFactory<AppDbContext>((serviceProvider, options) =>
        {
            var paths = serviceProvider.GetRequiredService<ApplicationDataPaths>();
            Directory.CreateDirectory(paths.RootDirectory);
            options.UseSqlite($"Data Source={paths.DatabasePath};Pooling=False");
        });

        services.AddHostedService<DatabaseInitializer>();
        services.AddScoped<IConnectionProfileService, ConnectionProfileService>();
        services.AddScoped<ICredentialStore, DpapiCredentialStore>();
        services.AddScoped<IFirebirdConnectionService, FirebirdConnectionService>();
        services.AddSingleton<IFirebirdCapabilitiesResolver, FirebirdCapabilitiesResolver>();
        services.AddSingleton<IFirebirdToolsetDiscoveryService, FirebirdToolsetDiscoveryService>();
        services.AddScoped<IMonitoringQueryStrategy, FirebirdMonitoringQueryStrategy>();

        return services;
    }
}

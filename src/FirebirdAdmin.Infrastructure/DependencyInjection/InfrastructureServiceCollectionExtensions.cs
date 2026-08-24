using FirebirdAdmin.Application.Connections;
using FirebirdAdmin.Application.Diagnostics;
using FirebirdAdmin.Application.History;
using FirebirdAdmin.Application.Maintenance;
using FirebirdAdmin.Application.Metadata;
using FirebirdAdmin.Application.Monitoring;
using FirebirdAdmin.Application.Profiler;
using FirebirdAdmin.Infrastructure.Connections;
using FirebirdAdmin.Infrastructure.Diagnostics;
using FirebirdAdmin.Infrastructure.History;
using FirebirdAdmin.Infrastructure.Maintenance;
using FirebirdAdmin.Infrastructure.Metadata;
using FirebirdAdmin.Infrastructure.Monitoring;
using FirebirdAdmin.Infrastructure.Persistence;
using FirebirdAdmin.Infrastructure.Profiler;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
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
            options
                .UseSqlite($"Data Source={paths.DatabasePath};Pooling=False")
                .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        services.AddHostedService<DatabaseInitializer>();
        services.AddScoped<IConnectionProfileService, ConnectionProfileService>();
        services.AddScoped<ICredentialStore, DpapiCredentialStore>();
        services.AddScoped<IFirebirdConnectionService, FirebirdConnectionService>();
        services.AddSingleton<IFirebirdCapabilitiesResolver, FirebirdCapabilitiesResolver>();
        services.AddSingleton<IFirebirdToolsetDiscoveryService, FirebirdToolsetDiscoveryService>();
        services.AddScoped<IMonitoringQueryStrategy, FirebirdMonitoringQueryStrategy>();
        services.AddScoped<IMetadataQueryStrategy, FirebirdMetadataQueryStrategy>();
        services.AddSingleton<SqliteConnectionFactory>();
        services.AddScoped<IHistoryWriter, DapperHistoryWriter>();
        services.AddScoped<IHistoryQueryService, DapperHistoryQueryService>();
        services.AddScoped<IRetentionPolicyService, SqliteRetentionPolicyService>();
        services.AddScoped<IHistoryExportService, HistoryExportService>();
        services.AddScoped<IAlertStore, SqliteAlertStore>();
        services.AddSingleton<IFirebirdToolRunner, FirebirdToolRunner>();
        services.AddScoped<IMaintenanceHistoryStore, SqliteMaintenanceHistoryStore>();
        services.AddSingleton<InAppNotificationChannel>();
        services.AddSingleton<INotificationChannel>(serviceProvider => serviceProvider.GetRequiredService<InAppNotificationChannel>());
        services.AddSingleton<ITraceConfigurationBuilder, TraceConfigurationBuilder>();
        services.AddSingleton<ITraceProcessRunner, TraceProcessRunner>();
        services.AddSingleton<IProfilerSessionService, FbTraceManagerProfilerSessionService>();

        return services;
    }
}

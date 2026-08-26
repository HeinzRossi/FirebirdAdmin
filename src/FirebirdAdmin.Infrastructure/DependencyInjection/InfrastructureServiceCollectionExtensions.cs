using FirebirdAdmin.Application.Connections;
using FirebirdAdmin.Application.Diagnostics;
using FirebirdAdmin.Application.History;
using FirebirdAdmin.Application.Maintenance;
using FirebirdAdmin.Application.Metadata;
using FirebirdAdmin.Application.Monitoring;
using FirebirdAdmin.Application.Profiler;
using FirebirdAdmin.Application.Security;
using FirebirdAdmin.Infrastructure.Connections;
using FirebirdAdmin.Infrastructure.Diagnostics;
using FirebirdAdmin.Infrastructure.History;
using FirebirdAdmin.Infrastructure.Maintenance;
using FirebirdAdmin.Infrastructure.Metadata;
using FirebirdAdmin.Infrastructure.Monitoring;
using FirebirdAdmin.Infrastructure.Persistence;
using FirebirdAdmin.Infrastructure.Profiler;
using FirebirdAdmin.Infrastructure.Security;
using Microsoft.Data.Sqlite;
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
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = paths.DatabasePath,
                Pooling = false,
                Mode = SqliteOpenMode.ReadWriteCreate
            }.ToString();
            options
                .UseSqlite(connectionString)
                .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        services.AddHostedService<DatabaseInitializer>();
        services.AddSingleton<IConnectionProfileService, ConnectionProfileService>();
        services.AddSingleton<ICredentialStore, DpapiCredentialStore>();
        services.AddSingleton<IFirebirdConnectionService, FirebirdConnectionService>();
        services.AddSingleton<IFirebirdCapabilitiesResolver, FirebirdCapabilitiesResolver>();
        services.AddSingleton<IFirebirdToolsetDiscoveryService, FirebirdToolsetDiscoveryService>();
        services.AddSingleton<IMonitoringQueryStrategy, FirebirdMonitoringQueryStrategy>();
        services.AddSingleton<IMetadataQueryStrategy, FirebirdMetadataQueryStrategy>();
        services.AddSingleton<ISecurityQueryStrategy, FirebirdSecurityQueryStrategy>();
        services.AddSingleton<SqliteConnectionFactory>();
        services.AddSingleton<DapperHistoryWriter>();
        services.AddSingleton<BufferedHistoryWriter>();
        services.AddSingleton<IHistoryWriter>(serviceProvider => serviceProvider.GetRequiredService<BufferedHistoryWriter>());
        services.AddHostedService(serviceProvider => serviceProvider.GetRequiredService<BufferedHistoryWriter>());
        services.AddSingleton<IHistoryQueryService, DapperHistoryQueryService>();
        services.AddSingleton<IRetentionPolicyService, SqliteRetentionPolicyService>();
        services.AddHostedService<HistoryRetentionHostedService>();
        services.AddSingleton<IHistoryExportService, HistoryExportService>();
        services.AddSingleton<IAlertStore, SqliteAlertStore>();
        services.AddSingleton<IFirebirdToolRunner, FirebirdToolRunner>();
        services.AddSingleton<IMaintenanceHistoryStore, SqliteMaintenanceHistoryStore>();
        services.AddSingleton<InAppNotificationChannel>();
        services.AddSingleton<INotificationChannel>(serviceProvider => serviceProvider.GetRequiredService<InAppNotificationChannel>());
        services.AddSingleton<ITraceConfigurationBuilder, TraceConfigurationBuilder>();
        services.AddSingleton<ITraceProcessRunner, TraceProcessRunner>();
        services.AddSingleton<IProfilerSessionService, FbTraceManagerProfilerSessionService>();

        return services;
    }
}

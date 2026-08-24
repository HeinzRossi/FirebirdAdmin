using Microsoft.Extensions.DependencyInjection;
using FirebirdAdmin.Application.Dashboard;
using FirebirdAdmin.Application.Diagnostics;
using FirebirdAdmin.Application.Diagnostics.Rules;
using FirebirdAdmin.Application.Metadata;
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
        services.AddSingleton<IDiagnosticRule, LongTransactionRule>();
        services.AddSingleton<IDiagnosticRule, AttachmentPressureRule>();
        services.AddSingleton<IDiagnosticRule, SlowStatementRule>();
        services.AddSingleton<IDiagnosticRule, TraceTechnicalErrorRule>();
        services.AddSingleton<IDiagnosticRule, StaleSnapshotRule>();
        services.AddSingleton<IDiagnosticEngine, DiagnosticEngine>();
        services.AddSingleton<IAlertCorrelator, AlertCorrelator>();
        services.AddSingleton<IMetadataCache, MetadataCache>();
        services.AddSingleton<IMetadataDdlBuilder, MetadataDdlBuilder>();
        services.AddScoped<IMetadataCatalogService, MetadataCatalogService>();
        return services;
    }
}

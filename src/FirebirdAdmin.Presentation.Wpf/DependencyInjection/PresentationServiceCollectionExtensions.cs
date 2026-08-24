using FirebirdAdmin.Presentation.Wpf.Dashboard;
using FirebirdAdmin.Presentation.Wpf.Diagnostics;
using FirebirdAdmin.Presentation.Wpf.History;
using FirebirdAdmin.Presentation.Wpf.Maintenance;
using FirebirdAdmin.Presentation.Wpf.Metadata;
using FirebirdAdmin.Presentation.Wpf.Shell;
using FirebirdAdmin.Presentation.Wpf.Monitoring;
using FirebirdAdmin.Presentation.Wpf.Profiler;
using FirebirdAdmin.Presentation.Wpf.Security;
using Microsoft.Extensions.DependencyInjection;

namespace FirebirdAdmin.Presentation.Wpf.DependencyInjection;

public static class PresentationServiceCollectionExtensions
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddSingleton<ShellViewModel>();
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<TransactionsWorkspaceViewModel>();
        services.AddSingleton<ProfilerWorkspaceViewModel>();
        services.AddSingleton<HistoryWorkspaceViewModel>();
        services.AddSingleton<AlertsCenterViewModel>();
        services.AddSingleton<MetadataExplorerViewModel>();
        services.AddSingleton<MaintenanceWorkspaceViewModel>();
        services.AddSingleton<SecurityWorkspaceViewModel>();
        services.AddSingleton<MainWindow>();

        return services;
    }
}

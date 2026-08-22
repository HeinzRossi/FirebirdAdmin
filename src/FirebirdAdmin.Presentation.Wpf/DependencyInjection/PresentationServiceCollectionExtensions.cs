using FirebirdAdmin.Presentation.Wpf.Dashboard;
using FirebirdAdmin.Presentation.Wpf.Shell;
using FirebirdAdmin.Presentation.Wpf.Monitoring;
using FirebirdAdmin.Presentation.Wpf.Profiler;
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
        services.AddSingleton<MainWindow>();

        return services;
    }
}

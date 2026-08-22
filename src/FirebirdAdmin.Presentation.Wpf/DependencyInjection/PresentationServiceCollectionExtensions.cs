using FirebirdAdmin.Presentation.Wpf.Shell;
using Microsoft.Extensions.DependencyInjection;

namespace FirebirdAdmin.Presentation.Wpf.DependencyInjection;

public static class PresentationServiceCollectionExtensions
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddSingleton<ShellViewModel>();
        services.AddSingleton<MainWindow>();

        return services;
    }
}

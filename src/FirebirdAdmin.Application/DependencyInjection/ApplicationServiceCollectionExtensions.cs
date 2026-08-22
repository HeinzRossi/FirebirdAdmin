using Microsoft.Extensions.DependencyInjection;

namespace FirebirdAdmin.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}

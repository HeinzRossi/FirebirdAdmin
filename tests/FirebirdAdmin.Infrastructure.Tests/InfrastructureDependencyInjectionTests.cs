using FirebirdAdmin.Application.DependencyInjection;
using FirebirdAdmin.Application.Maintenance;
using FirebirdAdmin.Application.Monitoring;
using FirebirdAdmin.Infrastructure.DependencyInjection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace FirebirdAdmin.Infrastructure.Tests;

public sealed class InfrastructureDependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_ShouldReturnSameServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddInfrastructure();

        result.Should().BeSameAs(services);
    }

    [Fact]
    public void Container_ShouldValidateSingletonLifetimes()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddInfrastructure();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        provider.GetRequiredService<IMonitoringSessionService>().Should().NotBeNull();
        provider.GetRequiredService<IMaintenanceService>().Should().NotBeNull();
    }
}

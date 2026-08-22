using FirebirdAdmin.Application.DependencyInjection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace FirebirdAdmin.Application.Tests;

public sealed class ApplicationDependencyInjectionTests
{
    [Fact]
    public void AddApplication_ShouldReturnSameServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddApplication();

        result.Should().BeSameAs(services);
    }
}

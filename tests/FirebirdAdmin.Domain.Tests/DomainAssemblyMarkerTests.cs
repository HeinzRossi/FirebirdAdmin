using FirebirdAdmin.Domain;
using FluentAssertions;

namespace FirebirdAdmin.Domain.Tests;

public sealed class DomainAssemblyMarkerTests
{
    [Fact]
    public void DomainAssemblyMarker_ShouldExposeDomainAssembly()
    {
        typeof(DomainAssemblyMarker).Assembly.GetName().Name.Should().Be("FirebirdAdmin.Domain");
    }
}

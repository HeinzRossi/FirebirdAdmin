using FirebirdAdmin.Application.Connections;
using FirebirdAdmin.Infrastructure.Connections;
using FluentAssertions;

namespace FirebirdAdmin.Infrastructure.Tests;

public sealed class FirebirdCapabilitiesResolverTests
{
    [Theory]
    [InlineData("2.5.9", true, false, false)]
    [InlineData("3.0.12", true, true, false)]
    [InlineData("4.0.5", true, true, true)]
    [InlineData("5.0.2", true, true, true)]
    public void Resolve_ShouldMapKnownVersions(string rawVersion, bool trace, bool packages, bool sqlSecurity)
    {
        var resolver = new FirebirdCapabilitiesResolver();

        var capabilities = resolver.Resolve(FirebirdServerVersion.Parse(rawVersion));

        capabilities.SupportsTrace.Should().Be(trace);
        capabilities.SupportsPackages.Should().Be(packages);
        capabilities.SupportsStandaloneFunctions.Should().Be(packages);
        capabilities.SupportsIdentityColumns.Should().Be(packages);
        capabilities.SupportsSqlSecurity.Should().Be(sqlSecurity);
    }

    [Fact]
    public void Resolve_ShouldDisableFeaturesForUnknownVersion()
    {
        var resolver = new FirebirdCapabilitiesResolver();

        var capabilities = resolver.Resolve(FirebirdServerVersion.Parse("not a version"));

        capabilities.SupportsTrace.Should().BeFalse();
        capabilities.SupportsPackages.Should().BeFalse();
    }
}

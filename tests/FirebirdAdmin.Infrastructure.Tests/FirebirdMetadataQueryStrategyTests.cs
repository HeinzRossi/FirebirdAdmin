using FirebirdAdmin.Application.Connections;
using FirebirdAdmin.Application.Metadata;
using FirebirdAdmin.Infrastructure.Metadata;
using FluentAssertions;

namespace FirebirdAdmin.Infrastructure.Tests;

public sealed class FirebirdMetadataQueryStrategyTests
{
    [Fact]
    public void GetSupportedKinds_ShouldHidePackagesAndFunctionsForFirebird25()
    {
        var kinds = FirebirdMetadataQueryStrategy.GetSupportedKinds(
            new FirebirdCapabilities(true, false, false, false, false, "2.5"));

        kinds.Should().Contain(MetadataObjectKind.Sequence);
        kinds.Should().NotContain(MetadataObjectKind.Package);
        kinds.Should().NotContain(MetadataObjectKind.Function);
    }

    [Fact]
    public void GetSupportedKinds_ShouldIncludePackagesAndFunctionsWhenCapabilitiesAllow()
    {
        var kinds = FirebirdMetadataQueryStrategy.GetSupportedKinds(
            new FirebirdCapabilities(true, true, true, true, true, "5.0"));

        kinds.Should().Contain(MetadataObjectKind.Package);
        kinds.Should().Contain(MetadataObjectKind.Function);
    }
}

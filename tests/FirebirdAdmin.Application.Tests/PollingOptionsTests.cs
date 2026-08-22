using FirebirdAdmin.Application.Monitoring;
using FluentAssertions;

namespace FirebirdAdmin.Application.Tests;

public sealed class PollingOptionsTests
{
    [Fact]
    public void Presets_ShouldUseDocumentedRanges()
    {
        PollingOptions.Aggressive.MinInterval.Should().Be(TimeSpan.FromMilliseconds(250));
        PollingOptions.Aggressive.MaxInterval.Should().Be(TimeSpan.FromSeconds(2));
        PollingOptions.Normal.MinInterval.Should().Be(TimeSpan.FromMilliseconds(500));
        PollingOptions.Normal.MaxInterval.Should().Be(TimeSpan.FromSeconds(5));
        PollingOptions.Conservative.MinInterval.Should().Be(TimeSpan.FromSeconds(1));
        PollingOptions.Conservative.MaxInterval.Should().Be(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void CreateCustom_ShouldRejectInvalidRange()
    {
        var act = () => PollingOptions.CreateCustom(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}

using FirebirdAdmin.Application.Connections;
using FluentAssertions;

namespace FirebirdAdmin.Application.Tests;

public sealed class FirebirdServerVersionTests
{
    [Theory]
    [InlineData("WI-V5.0.2.1613 Firebird 5.0", 5, 0, 2)]
    [InlineData("3.0.12", 3, 0, 12)]
    [InlineData("2.5", 2, 5, 0)]
    public void Parse_ShouldExtractVersionNumbers(string raw, int major, int minor, int patch)
    {
        var version = FirebirdServerVersion.Parse(raw);

        version.Major.Should().Be(major);
        version.Minor.Should().Be(minor);
        version.Patch.Should().Be(patch);
        version.Raw.Should().Be(raw);
    }

    [Fact]
    public void Parse_ShouldReturnUnknownForInvalidText()
    {
        var version = FirebirdServerVersion.Parse("unknown");

        version.Major.Should().Be(0);
        version.Raw.Should().Be("unknown");
    }
}

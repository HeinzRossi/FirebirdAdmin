using FirebirdAdmin.Application.Connections;
using FirebirdAdmin.Infrastructure.Connections;
using FluentAssertions;

namespace FirebirdAdmin.Infrastructure.Tests;

public sealed class FirebirdToolsetDiscoveryServiceTests
{
    [Fact]
    public async Task DiscoverAsync_ShouldFindToolsFromFirebirdHome()
    {
        using var fixture = TempDirectoryFixture.Create();
        File.WriteAllText(Path.Combine(fixture.Path, "fbclient.dll"), string.Empty);
        File.WriteAllText(Path.Combine(fixture.Path, "gbak.exe"), string.Empty);
        File.WriteAllText(Path.Combine(fixture.Path, "gfix.exe"), string.Empty);
        File.WriteAllText(Path.Combine(fixture.Path, "fbtracemgr.exe"), string.Empty);

        var previous = Environment.GetEnvironmentVariable("FIREBIRD_HOME");
        Environment.SetEnvironmentVariable("FIREBIRD_HOME", fixture.Path);

        try
        {
            var service = new FirebirdToolsetDiscoveryService();

            var toolset = await service.DiscoverAsync(CancellationToken.None);

            toolset.Candidates.Should().Contain(candidate => candidate.Kind == FirebirdToolKind.ClientLibrary && candidate.IsAvailable);
            toolset.Candidates.Should().Contain(candidate => candidate.Kind == FirebirdToolKind.Backup && candidate.IsAvailable);
            toolset.Candidates.Should().Contain(candidate => candidate.Kind == FirebirdToolKind.Fix && candidate.IsAvailable);
            toolset.Candidates.Should().Contain(candidate => candidate.Kind == FirebirdToolKind.TraceManager && candidate.IsAvailable);
        }
        finally
        {
            Environment.SetEnvironmentVariable("FIREBIRD_HOME", previous);
        }
    }
}

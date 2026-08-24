using FirebirdAdmin.Application.Connections;
using FirebirdAdmin.Application.Security;
using FirebirdAdmin.Presentation.Wpf.Security;
using FluentAssertions;

namespace FirebirdAdmin.Ui.Tests;

public sealed class SecurityWorkspaceViewModelTests
{
    [Fact]
    public void Constructor_ShouldStartEmpty()
    {
        var viewModel = new SecurityWorkspaceViewModel(new FakeSecurityCatalogService());

        viewModel.State.Should().Be(SecurityCacheState.Empty);
        viewModel.Principals.Should().BeEmpty();
        viewModel.Grants.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulatePrincipalsAndGrants()
    {
        var viewModel = new SecurityWorkspaceViewModel(new FakeSecurityCatalogService());
        viewModel.SetConnection(CreateConnection(), null);

        await viewModel.LoadAsync();

        viewModel.State.Should().Be(SecurityCacheState.Fresh);
        viewModel.Principals.Should().HaveCount(2);
        viewModel.Grants.Should().HaveCount(2);
    }

    [Fact]
    public async Task Filter_ShouldUpdateGrants()
    {
        var viewModel = new SecurityWorkspaceViewModel(new FakeSecurityCatalogService());
        viewModel.SetConnection(CreateConnection(), null);
        await viewModel.LoadAsync();

        viewModel.FilterText = "customer";

        viewModel.Grants.Should().ContainSingle();
        viewModel.Grants[0].Object.Should().Contain("CUSTOMERS");
    }

    [Fact]
    public async Task MarkStale_ShouldKeepCachedCatalog()
    {
        var service = new FakeSecurityCatalogService();
        var viewModel = new SecurityWorkspaceViewModel(service);
        viewModel.SetConnection(CreateConnection(), null);
        await viewModel.LoadAsync();

        viewModel.MarkStale();

        viewModel.State.Should().Be(SecurityCacheState.Stale);
        viewModel.Grants.Should().HaveCount(2);
    }

    private static ConnectionContext CreateConnection()
    {
        return new ConnectionContext(
            Guid.NewGuid(),
            "Local",
            "localhost",
            3050,
            "db.fdb",
            "SYSDBA",
            FirebirdServerVersion.Parse("5.0.0"),
            new FirebirdCapabilities(true, true, true, true, true, "test"),
            EffectiveToolset.Empty,
            DateTimeOffset.UtcNow);
    }

    private sealed class FakeSecurityCatalogService : ISecurityCatalogService
    {
        private SecurityCatalog? catalog;

        public Task<SecurityCatalog> LoadCatalogAsync(ConnectionContext connection, CredentialSecret? password, CancellationToken cancellationToken)
        {
            catalog = new SecurityCatalog(
                [new SecurityUser("ALICE", "SEC$USERS", true)],
                [new SecurityRole("REPORTING", "SYSDBA")],
                [
                    new SecurityGrant(new SecurityPrincipalReference("ALICE", "User"), new SecurityObjectReference("CUSTOMERS", "Table"), SecurityPrivilege.FromCode("S"), "SYSDBA", false, SecurityGrantKind.ObjectPrivilege),
                    new SecurityGrant(new SecurityPrincipalReference("ALICE", "User"), new SecurityObjectReference("REPORTING", "Role"), SecurityPrivilege.FromCode("M"), "SYSDBA", false, SecurityGrantKind.RoleMembership)
                ],
                DateTimeOffset.UtcNow,
                SecurityCacheState.Fresh);
            return Task.FromResult(catalog);
        }

        public SecurityCatalog? GetCachedCatalog() => catalog;

        public void MarkCacheStale()
        {
            if (catalog is not null)
            {
                catalog = catalog with { State = SecurityCacheState.Stale };
            }
        }
    }
}

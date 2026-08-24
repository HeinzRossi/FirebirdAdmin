using FirebirdAdmin.Application.Security;
using FluentAssertions;

namespace FirebirdAdmin.Application.Tests;

public sealed class SecurityTests
{
    [Fact]
    public void Catalog_ShouldExposeMembershipsAndSearchGrants()
    {
        var catalog = CreateCatalog();

        catalog.Memberships.Should().ContainSingle();
        catalog.SearchGrants("customer").Should().ContainSingle(grant => grant.Privilege.Name == "SELECT");
        catalog.SearchGrants("member").Single().Kind.Should().Be(SecurityGrantKind.RoleMembership);
    }

    [Fact]
    public void Cache_ShouldBecomeStaleWithoutLosingCatalog()
    {
        var cache = new SecurityCache();
        cache.Store(CreateCatalog());

        cache.MarkStale();

        cache.Current.Should().NotBeNull();
        cache.Current!.State.Should().Be(SecurityCacheState.Stale);
        cache.Current.Grants.Should().HaveCount(2);
    }

    [Theory]
    [InlineData("S", "SELECT")]
    [InlineData("I", "INSERT")]
    [InlineData("U", "UPDATE")]
    [InlineData("D", "DELETE")]
    [InlineData("R", "REFERENCES")]
    [InlineData("X", "EXECUTE")]
    [InlineData("A", "ALL")]
    [InlineData("G", "USAGE")]
    [InlineData("M", "MEMBER OF")]
    [InlineData("C", "CREATE")]
    [InlineData("L", "ALTER")]
    [InlineData("O", "DROP")]
    public void Privilege_ShouldMapKnownCodes(string code, string name)
    {
        SecurityPrivilege.FromCode(code).Name.Should().Be(name);
    }

    private static SecurityCatalog CreateCatalog()
    {
        return new SecurityCatalog(
            [new SecurityUser("ALICE", "SEC$USERS", true)],
            [new SecurityRole("REPORTING", "SYSDBA")],
            [
                new SecurityGrant(new SecurityPrincipalReference("ALICE", "User"), new SecurityObjectReference("CUSTOMERS", "Table"), SecurityPrivilege.FromCode("S"), "SYSDBA", false, SecurityGrantKind.ObjectPrivilege),
                new SecurityGrant(new SecurityPrincipalReference("ALICE", "User"), new SecurityObjectReference("REPORTING", "Role"), SecurityPrivilege.FromCode("M"), "SYSDBA", false, SecurityGrantKind.RoleMembership)
            ],
            DateTimeOffset.UtcNow,
            SecurityCacheState.Fresh);
    }
}

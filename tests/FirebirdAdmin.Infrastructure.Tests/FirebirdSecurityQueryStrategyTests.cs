using FirebirdAdmin.Application.Security;
using FirebirdAdmin.Infrastructure.Security;
using FluentAssertions;

namespace FirebirdAdmin.Infrastructure.Tests;

public sealed class FirebirdSecurityQueryStrategyTests
{
    [Fact]
    public void GetReadOnlyQueryNames_ShouldUseOnlySecuritySystemTables()
    {
        var names = FirebirdSecurityQueryStrategy.GetReadOnlyQueryNames();

        names.Should().Contain(["SEC$USERS", "RDB$ROLES", "RDB$USER_PRIVILEGES"]);
    }

    [Fact]
    public void Source_ShouldNotContainMutableSecurityCommands()
    {
        var source = File.ReadAllText(FindRepoFile("src", "FirebirdAdmin.Infrastructure", "Security", "FirebirdSecurityQueryStrategy.cs"));

        source.Should().NotContain("CREATE USER");
        source.Should().NotContain("ALTER USER");
        source.Should().NotContain("DROP USER");
        source.Should().NotContain("GRANT ");
        source.Should().NotContain("REVOKE ");
    }

    [Fact]
    public void PrivilegeM_ShouldBeRoleMembership()
    {
        var grant = new SecurityGrant(
            new SecurityPrincipalReference("ALICE", "User"),
            new SecurityObjectReference("REPORTING", "Role"),
            SecurityPrivilege.FromCode("M"),
            "SYSDBA",
            false,
            SecurityGrantKind.RoleMembership);

        grant.Kind.Should().Be(SecurityGrantKind.RoleMembership);
        grant.Privilege.Name.Should().Be("MEMBER OF");
    }

    private static string FindRepoFile(params string[] parts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine([directory.FullName, .. parts]);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(string.Join(Path.DirectorySeparatorChar, parts));
    }
}

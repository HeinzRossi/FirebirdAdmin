using FirebirdAdmin.Application.Connections;
using FirebirdAdmin.Application.Security;
using FirebirdSql.Data.FirebirdClient;

namespace FirebirdAdmin.Infrastructure.Security;

public sealed class FirebirdSecurityQueryStrategy : ISecurityQueryStrategy
{
    public static IReadOnlyList<string> GetReadOnlyQueryNames()
    {
        return ["SEC$USERS", "RDB$ROLES", "RDB$USER_PRIVILEGES"];
    }

    public async Task<SecurityCatalog> LoadCatalogAsync(ConnectionContext connection, CredentialSecret? password, CancellationToken cancellationToken)
    {
        await using var fbConnection = new FbConnection(BuildConnectionString(connection, password));
        await fbConnection.OpenAsync(cancellationToken);

        var warnings = new List<string>();
        IReadOnlyList<SecurityGrant> grants;
        IReadOnlyList<SecurityRole> roles;
        IReadOnlyList<SecurityUser> users;

        try
        {
            roles = await LoadRolesAsync(fbConnection, cancellationToken);
            grants = await LoadGrantsAsync(fbConnection, cancellationToken);
        }
        catch (Exception ex)
        {
            return new SecurityCatalog([], [], [], DateTimeOffset.UtcNow, SecurityCacheState.Fresh, Error: ex.Message);
        }

        try
        {
            users = await LoadUsersAsync(fbConnection, cancellationToken);
        }
        catch (Exception ex)
        {
            warnings.Add($"SEC$USERS indisponível; usuários inferidos de grants. {ex.Message}");
            users = InferUsers(grants);
        }

        return new SecurityCatalog(users, roles, grants, DateTimeOffset.UtcNow, SecurityCacheState.Fresh, string.Join(Environment.NewLine, warnings.Where(item => item.Length > 0)));
    }

    private static string BuildConnectionString(ConnectionContext connection, CredentialSecret? password)
    {
        return new FbConnectionStringBuilder
        {
            DataSource = connection.Host,
            Port = connection.Port,
            Database = connection.Database,
            UserID = connection.UserName,
            Password = password?.RevealAsString() ?? string.Empty,
            Pooling = false
        }.ToString();
    }

    private static async Task<IReadOnlyList<SecurityUser>> LoadUsersAsync(FbConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            select trim(sec$user_name), sec$first_name, sec$last_name
            from sec$users
            order by 1
            """;

        var users = new List<SecurityUser>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            users.Add(new SecurityUser(
                reader.GetString(0).Trim(),
                "SEC$USERS",
                IsVisible: true,
                reader.IsDBNull(1) ? null : reader.GetString(1).Trim(),
                reader.IsDBNull(2) ? null : reader.GetString(2).Trim()));
        }

        return users;
    }

    private static async Task<IReadOnlyList<SecurityRole>> LoadRolesAsync(FbConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            select trim(rdb$role_name), trim(rdb$owner_name), coalesce(rdb$system_flag, 0)
            from rdb$roles
            order by 1
            """;

        var roles = new List<SecurityRole>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            roles.Add(new SecurityRole(
                reader.GetString(0).Trim(),
                reader.IsDBNull(1) ? null : reader.GetString(1).Trim(),
                Convert.ToInt32(reader.GetValue(2)) != 0));
        }

        return roles;
    }

    private static async Task<IReadOnlyList<SecurityGrant>> LoadGrantsAsync(FbConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            select
                trim(rdb$user),
                trim(rdb$grantor),
                trim(rdb$privilege),
                coalesce(rdb$grant_option, 0),
                trim(rdb$relation_name),
                trim(rdb$field_name),
                rdb$user_type,
                rdb$object_type
            from rdb$user_privileges
            order by rdb$user, rdb$relation_name, rdb$privilege
            """;

        var grants = new List<SecurityGrant>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var privilege = SecurityPrivilege.FromCode(reader.GetString(2));
            grants.Add(new SecurityGrant(
                new SecurityPrincipalReference(reader.GetString(0).Trim(), MapUserType(reader.IsDBNull(6) ? null : Convert.ToInt32(reader.GetValue(6)))),
                new SecurityObjectReference(
                    reader.IsDBNull(4) ? null : reader.GetString(4).Trim(),
                    MapObjectType(reader.IsDBNull(7) ? null : Convert.ToInt32(reader.GetValue(7))),
                    reader.IsDBNull(5) ? null : reader.GetString(5).Trim()),
                privilege,
                reader.IsDBNull(1) ? string.Empty : reader.GetString(1).Trim(),
                Convert.ToInt32(reader.GetValue(3)) == 1,
                MapGrantKind(privilege)));
        }

        return grants;
    }

    private static IReadOnlyList<SecurityUser> InferUsers(IReadOnlyList<SecurityGrant> grants)
    {
        return grants
            .Where(grant => string.Equals(grant.Principal.Type, "User", StringComparison.OrdinalIgnoreCase))
            .Select(grant => grant.Principal.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name)
            .Select(name => new SecurityUser(name, "RDB$USER_PRIVILEGES", IsVisible: false))
            .ToArray();
    }

    private static SecurityGrantKind MapGrantKind(SecurityPrivilege privilege)
    {
        return privilege.Code switch
        {
            "M" => SecurityGrantKind.RoleMembership,
            "C" or "L" or "O" => SecurityGrantKind.DdlPrivilege,
            "?" => SecurityGrantKind.Unknown,
            _ => SecurityGrantKind.ObjectPrivilege
        };
    }

    private static string MapUserType(int? type)
    {
        return type switch
        {
            8 => "User",
            13 => "Role",
            5 => "Procedure",
            1 => "View",
            2 => "Trigger",
            _ => type is null ? "Unknown" : $"Type {type}"
        };
    }

    private static string MapObjectType(int? type)
    {
        return type switch
        {
            0 => "Table",
            1 => "View",
            2 => "Trigger",
            5 => "Procedure",
            7 => "Exception",
            8 => "User",
            9 => "Domain",
            11 => "CharacterSet",
            13 => "Role",
            14 => "Sequence",
            15 => "Function",
            16 => "BlobFilter",
            17 => "Collation",
            18 => "Package",
            _ => type is null ? "Unknown" : $"Type {type}"
        };
    }
}

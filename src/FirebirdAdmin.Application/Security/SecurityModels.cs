namespace FirebirdAdmin.Application.Security;

public sealed record SecurityPrincipalReference(
    string Name,
    string Type);

public sealed record SecurityObjectReference(
    string? Name,
    string Type,
    string? FieldName = null);

public sealed record SecurityUser(
    string Name,
    string Source,
    bool IsVisible,
    string? FirstName = null,
    string? LastName = null);

public sealed record SecurityRole(
    string Name,
    string? Owner,
    bool IsSystemRole = false);

public sealed record SecurityGrant(
    SecurityPrincipalReference Principal,
    SecurityObjectReference Object,
    SecurityPrivilege Privilege,
    string Grantor,
    bool WithGrantOption,
    SecurityGrantKind Kind);

public sealed record SecurityCatalog(
    IReadOnlyList<SecurityUser> Users,
    IReadOnlyList<SecurityRole> Roles,
    IReadOnlyList<SecurityGrant> Grants,
    DateTimeOffset LoadedAt,
    SecurityCacheState State,
    string? Warning = null,
    string? Error = null)
{
    public IReadOnlyList<SecurityGrant> Memberships => Grants.Where(grant => grant.Kind is SecurityGrantKind.RoleMembership).ToArray();

    public IReadOnlyList<SecurityGrant> SearchGrants(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Grants;
        }

        return Grants.Where(grant =>
            grant.Principal.Name.Contains(text, StringComparison.OrdinalIgnoreCase) ||
            grant.Principal.Type.Contains(text, StringComparison.OrdinalIgnoreCase) ||
            grant.Object.Type.Contains(text, StringComparison.OrdinalIgnoreCase) ||
            (grant.Object.Name?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false) ||
            grant.Privilege.Name.Contains(text, StringComparison.OrdinalIgnoreCase) ||
            grant.Grantor.Contains(text, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }
}

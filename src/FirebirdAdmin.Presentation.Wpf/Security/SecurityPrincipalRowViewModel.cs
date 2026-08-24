using FirebirdAdmin.Application.Security;

namespace FirebirdAdmin.Presentation.Wpf.Security;

public sealed class SecurityPrincipalRowViewModel
{
    public SecurityPrincipalRowViewModel(SecurityUser user)
    {
        Name = user.Name;
        Type = "User";
        Source = user.Source;
        Detail = user.IsVisible ? $"{user.FirstName} {user.LastName}".Trim() : "Inferido de grants";
    }

    public SecurityPrincipalRowViewModel(SecurityRole role)
    {
        Name = role.Name;
        Type = "Role";
        Source = role.IsSystemRole ? "System" : "RDB$ROLES";
        Detail = role.Owner ?? "-";
    }

    public string Name { get; }
    public string Type { get; }
    public string Source { get; }
    public string Detail { get; }
}

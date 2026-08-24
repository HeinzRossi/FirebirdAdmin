using FirebirdAdmin.Application.Security;

namespace FirebirdAdmin.Presentation.Wpf.Security;

public sealed class SecurityGrantRowViewModel(SecurityGrant grant)
{
    public SecurityGrant Grant { get; } = grant;
    public string Principal => $"{Grant.Principal.Type}: {Grant.Principal.Name}";
    public string Privilege => Grant.Privilege.Name;
    public string Object => $"{Grant.Object.Type}: {Grant.Object.Name ?? "-"}";
    public string Field => Grant.Object.FieldName ?? "-";
    public string Grantor => Grant.Grantor;
    public string Kind => Grant.Kind.ToString();
    public string GrantOption => Grant.WithGrantOption ? "Sim" : "Não";
}

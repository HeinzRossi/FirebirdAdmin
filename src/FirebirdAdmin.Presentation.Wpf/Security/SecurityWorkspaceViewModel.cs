using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FirebirdAdmin.Application.Connections;
using FirebirdAdmin.Application.Security;

namespace FirebirdAdmin.Presentation.Wpf.Security;

public sealed partial class SecurityWorkspaceViewModel(ISecurityCatalogService catalogService) : ObservableObject, IDisposable
{
    private ConnectionContext? activeConnection;
    private CredentialSecret? password;
    private SecurityCatalog? catalog;
    private bool disposed;

    [ObservableProperty]
    private SecurityCacheState state = SecurityCacheState.Empty;

    [ObservableProperty]
    private string message = "Conecte um banco para carregar segurança.";

    [ObservableProperty]
    private string filterText = string.Empty;

    [ObservableProperty]
    private SecurityGrantRowViewModel? selectedGrant;

    [ObservableProperty]
    private SecurityPrincipalRowViewModel? selectedPrincipal;

    public ObservableCollection<SecurityPrincipalRowViewModel> Principals { get; } = [];
    public ObservableCollection<SecurityGrantRowViewModel> Grants { get; } = [];
    public string StateText => State.ToString();
    public string SummaryText => catalog is null
        ? "Usuários: 0 | Roles: 0 | Grants: 0 | Memberships: 0"
        : $"Usuários: {catalog.Users.Count} | Roles: {catalog.Roles.Count} | Grants: {catalog.Grants.Count} | Memberships: {catalog.Memberships.Count}";
    public string SelectedDetails => SelectedGrant is not null
        ? $"Principal: {SelectedGrant.Principal}{Environment.NewLine}Privilégio: {SelectedGrant.Privilege}{Environment.NewLine}Objeto: {SelectedGrant.Object}{Environment.NewLine}Campo: {SelectedGrant.Field}{Environment.NewLine}Grantor: {SelectedGrant.Grantor}{Environment.NewLine}Grant option: {SelectedGrant.GrantOption}{Environment.NewLine}Tipo: {SelectedGrant.Kind}"
        : SelectedPrincipal is not null
            ? $"{SelectedPrincipal.Type}: {SelectedPrincipal.Name}{Environment.NewLine}Origem: {SelectedPrincipal.Source}{Environment.NewLine}{SelectedPrincipal.Detail}"
            : "-";

    public void SetConnection(ConnectionContext connection, CredentialSecret? credential)
    {
        activeConnection = connection;
        password?.Dispose();
        password = credential is null ? null : CredentialSecret.FromBytes(credential.CopyBytes());
        State = SecurityCacheState.Empty;
        Message = "Conexão ativa. Segurança pronta para carregar.";
        OnStateChanged();
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (activeConnection is null)
        {
            catalog = catalogService.GetCachedCatalog();
            ApplyCatalog();
            Message = catalog is null ? "Sem conexão ativa." : "Cache de segurança disponível.";
            return;
        }

        Message = "Carregando segurança read-only...";
        try
        {
            catalog = await catalogService.LoadCatalogAsync(activeConnection, password, cancellationToken);
            ApplyCatalog();
            Message = catalog.Error is null
                ? string.IsNullOrWhiteSpace(catalog.Warning) ? "Segurança carregada." : catalog.Warning
                : $"Falha: {catalog.Error}";
        }
        catch (Exception ex)
        {
            Message = ex.Message;
        }
    }

    public Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        return LoadAsync(cancellationToken);
    }

    public void MarkStale()
    {
        catalogService.MarkCacheStale();
        catalog = catalogService.GetCachedCatalog();
        ApplyCatalog();
        Message = catalog is null ? "Sem cache de segurança." : "Cache de segurança marcado como stale.";
    }

    partial void OnFilterTextChanged(string value)
    {
        ApplyCatalog();
    }

    partial void OnSelectedGrantChanged(SecurityGrantRowViewModel? value)
    {
        OnPropertyChanged(nameof(SelectedDetails));
    }

    partial void OnSelectedPrincipalChanged(SecurityPrincipalRowViewModel? value)
    {
        OnPropertyChanged(nameof(SelectedDetails));
    }

    private void ApplyCatalog()
    {
        Principals.Clear();
        Grants.Clear();

        if (catalog is null)
        {
            State = SecurityCacheState.Empty;
            OnStateChanged();
            return;
        }

        foreach (var user in catalog.Users.Where(MatchesUser).OrderBy(user => user.Name))
        {
            Principals.Add(new SecurityPrincipalRowViewModel(user));
        }

        foreach (var role in catalog.Roles.Where(MatchesRole).OrderBy(role => role.Name))
        {
            Principals.Add(new SecurityPrincipalRowViewModel(role));
        }

        foreach (var grant in catalog.SearchGrants(FilterText).OrderBy(grant => grant.Principal.Name).ThenBy(grant => grant.Object.Name))
        {
            Grants.Add(new SecurityGrantRowViewModel(grant));
        }

        State = catalog.State;
        OnStateChanged();
    }

    private bool MatchesUser(SecurityUser user)
    {
        return string.IsNullOrWhiteSpace(FilterText) ||
            user.Name.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ||
            user.Source.Contains(FilterText, StringComparison.OrdinalIgnoreCase);
    }

    private bool MatchesRole(SecurityRole role)
    {
        return string.IsNullOrWhiteSpace(FilterText) ||
            role.Name.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ||
            (role.Owner?.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private void OnStateChanged()
    {
        OnPropertyChanged(nameof(StateText));
        OnPropertyChanged(nameof(SummaryText));
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        password?.Dispose();
        disposed = true;
    }
}

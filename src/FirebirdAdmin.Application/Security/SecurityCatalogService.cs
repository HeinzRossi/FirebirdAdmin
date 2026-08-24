using FirebirdAdmin.Application.Connections;

namespace FirebirdAdmin.Application.Security;

public sealed class SecurityCatalogService(
    ISecurityQueryStrategy queryStrategy,
    ISecurityCache cache) : ISecurityCatalogService
{
    public async Task<SecurityCatalog> LoadCatalogAsync(ConnectionContext connection, CredentialSecret? password, CancellationToken cancellationToken)
    {
        var catalog = await queryStrategy.LoadCatalogAsync(connection, password, cancellationToken);
        cache.Store(catalog);
        return cache.Current!;
    }

    public SecurityCatalog? GetCachedCatalog()
    {
        return cache.Current;
    }

    public void MarkCacheStale()
    {
        cache.MarkStale();
    }
}

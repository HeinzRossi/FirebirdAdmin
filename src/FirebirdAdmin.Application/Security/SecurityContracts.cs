using FirebirdAdmin.Application.Connections;

namespace FirebirdAdmin.Application.Security;

public interface ISecurityQueryStrategy
{
    Task<SecurityCatalog> LoadCatalogAsync(ConnectionContext connection, CredentialSecret? password, CancellationToken cancellationToken);
}

public interface ISecurityCatalogService
{
    Task<SecurityCatalog> LoadCatalogAsync(ConnectionContext connection, CredentialSecret? password, CancellationToken cancellationToken);
    SecurityCatalog? GetCachedCatalog();
    void MarkCacheStale();
}

public interface ISecurityCache
{
    SecurityCatalog? Current { get; }
    void Store(SecurityCatalog catalog);
    void MarkStale();
}

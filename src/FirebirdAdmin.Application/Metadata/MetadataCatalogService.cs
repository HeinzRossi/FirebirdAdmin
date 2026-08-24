using FirebirdAdmin.Application.Connections;

namespace FirebirdAdmin.Application.Metadata;

public sealed class MetadataCatalogService(
    IMetadataQueryStrategy queryStrategy,
    IMetadataCache cache) : IMetadataCatalogService
{
    public async Task<MetadataCatalog> LoadCatalogAsync(ConnectionContext connection, CredentialSecret? password, CancellationToken cancellationToken)
    {
        var objects = await queryStrategy.LoadCatalogAsync(connection, password, cancellationToken);
        var catalog = new MetadataCatalog(objects, DateTimeOffset.UtcNow, MetadataCacheState.Fresh);
        cache.Store(catalog);
        return catalog;
    }

    public Task<MetadataObjectDetails> LoadDetailsAsync(ConnectionContext connection, CredentialSecret? password, MetadataObjectReference reference, CancellationToken cancellationToken)
    {
        return queryStrategy.LoadDetailsAsync(connection, password, reference, cancellationToken);
    }

    public MetadataCatalog? GetCachedCatalog()
    {
        return cache.Current;
    }

    public void MarkCacheStale()
    {
        cache.MarkStale();
    }
}

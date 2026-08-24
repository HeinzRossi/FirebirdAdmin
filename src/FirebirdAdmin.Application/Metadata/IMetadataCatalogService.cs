using FirebirdAdmin.Application.Connections;

namespace FirebirdAdmin.Application.Metadata;

public interface IMetadataCatalogService
{
    Task<MetadataCatalog> LoadCatalogAsync(ConnectionContext connection, CredentialSecret? password, CancellationToken cancellationToken);
    Task<MetadataObjectDetails> LoadDetailsAsync(ConnectionContext connection, CredentialSecret? password, MetadataObjectReference reference, CancellationToken cancellationToken);
    MetadataCatalog? GetCachedCatalog();
    void MarkCacheStale();
}

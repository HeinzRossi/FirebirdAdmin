namespace FirebirdAdmin.Application.Metadata;

public interface IMetadataCache
{
    MetadataCatalog? Current { get; }
    void Store(MetadataCatalog catalog);
    void MarkStale();
}

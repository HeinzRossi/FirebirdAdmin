namespace FirebirdAdmin.Application.Metadata;

public sealed class MetadataCache : IMetadataCache
{
    public MetadataCatalog? Current { get; private set; }

    public void Store(MetadataCatalog catalog)
    {
        Current = catalog with { State = MetadataCacheState.Fresh };
    }

    public void MarkStale()
    {
        if (Current is not null)
        {
            Current = Current with { State = MetadataCacheState.Stale };
        }
    }
}

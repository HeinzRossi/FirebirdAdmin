namespace FirebirdAdmin.Application.Security;

public sealed class SecurityCache : ISecurityCache
{
    public SecurityCatalog? Current { get; private set; }

    public void Store(SecurityCatalog catalog)
    {
        Current = catalog with { State = SecurityCacheState.Fresh };
    }

    public void MarkStale()
    {
        if (Current is not null)
        {
            Current = Current with { State = SecurityCacheState.Stale };
        }
    }
}

namespace FirebirdAdmin.Application.Connections;

public interface IFirebirdToolsetDiscoveryService
{
    Task<EffectiveToolset> DiscoverAsync(CancellationToken cancellationToken);
}

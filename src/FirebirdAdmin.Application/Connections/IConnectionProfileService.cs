namespace FirebirdAdmin.Application.Connections;

public interface IConnectionProfileService
{
    Task<IReadOnlyList<ConnectionProfile>> ListAsync(CancellationToken cancellationToken);
    Task<ConnectionProfile?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<ConnectionProfile> SaveAsync(ConnectionProfileRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

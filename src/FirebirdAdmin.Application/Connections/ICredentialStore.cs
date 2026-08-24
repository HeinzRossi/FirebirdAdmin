namespace FirebirdAdmin.Application.Connections;

public interface ICredentialStore
{
    Task SaveAsync(Guid profileId, CredentialSecret secret, CancellationToken cancellationToken);
    Task<CredentialSecret?> TryLoadAsync(Guid profileId, CancellationToken cancellationToken);
    Task DeleteAsync(Guid profileId, CancellationToken cancellationToken);
}

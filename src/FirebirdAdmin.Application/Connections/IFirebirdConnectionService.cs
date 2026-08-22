namespace FirebirdAdmin.Application.Connections;

public interface IFirebirdConnectionService
{
    Task<ConnectionContext> ConnectAsync(ConnectionRequest request, CancellationToken cancellationToken);
}

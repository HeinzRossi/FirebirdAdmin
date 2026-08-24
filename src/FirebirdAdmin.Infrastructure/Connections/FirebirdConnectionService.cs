using FirebirdAdmin.Application.Connections;
using FirebirdSql.Data.FirebirdClient;

namespace FirebirdAdmin.Infrastructure.Connections;

public sealed class FirebirdConnectionService(
    IFirebirdCapabilitiesResolver capabilitiesResolver,
    IFirebirdToolsetDiscoveryService toolsetDiscoveryService) : IFirebirdConnectionService
{
    public async Task<ConnectionContext> ConnectAsync(ConnectionRequest request, CancellationToken cancellationToken)
    {
        using var password = request.Password;
        var connectionString = BuildConnectionString(request.Profile, password);

        await using var connection = new FbConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var version = await DetectVersionAsync(connection, cancellationToken);
        var capabilities = capabilitiesResolver.Resolve(version);
        var toolset = await toolsetDiscoveryService.DiscoverAsync(cancellationToken);

        return new ConnectionContext(
            request.Profile.Id,
            request.Profile.Name,
            request.Profile.Host,
            request.Profile.Port,
            request.Profile.Database,
            request.Profile.UserName,
            version,
            capabilities,
            toolset,
            DateTimeOffset.UtcNow);
    }

    private static string BuildConnectionString(ConnectionProfile profile, CredentialSecret? password)
    {
        var builder = new FbConnectionStringBuilder
        {
            DataSource = profile.Host,
            Port = profile.Port,
            Database = profile.Database,
            UserID = profile.UserName,
            Password = password?.RevealAsString() ?? string.Empty,
            Pooling = false
        };

        if (!string.IsNullOrWhiteSpace(profile.Charset))
        {
            builder.Charset = profile.Charset;
        }

        if (!string.IsNullOrWhiteSpace(profile.Role))
        {
            builder.Role = profile.Role;
        }

        return builder.ToString();
    }

    private static async Task<FirebirdServerVersion> DetectVersionAsync(FbConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "select rdb$get_context('SYSTEM', 'ENGINE_VERSION') from rdb$database";

        var value = await command.ExecuteScalarAsync(cancellationToken);
        return FirebirdServerVersion.Parse(value?.ToString() ?? connection.ServerVersion);
    }
}

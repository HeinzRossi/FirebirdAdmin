namespace FirebirdAdmin.Application.Connections;

public interface IFirebirdCapabilitiesResolver
{
    FirebirdCapabilities Resolve(FirebirdServerVersion version);
}

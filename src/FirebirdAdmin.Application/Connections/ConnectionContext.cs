namespace FirebirdAdmin.Application.Connections;

public sealed record ConnectionContext(
    Guid ProfileId,
    string ProfileName,
    string Host,
    int Port,
    string Database,
    string UserName,
    FirebirdServerVersion ServerVersion,
    FirebirdCapabilities Capabilities,
    EffectiveToolset Toolset,
    DateTimeOffset ConnectedAt);

namespace FirebirdAdmin.Application.Connections;

public sealed record ConnectionRequest(
    ConnectionProfile Profile,
    CredentialSecret? Password);

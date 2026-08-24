namespace FirebirdAdmin.Application.Connections;

public sealed record ConnectionProfileRequest(
    Guid? Id,
    string Name,
    string Host,
    int Port,
    string Database,
    string UserName,
    string? Charset,
    string? Role,
    bool RememberPassword,
    CredentialSecret? Password);

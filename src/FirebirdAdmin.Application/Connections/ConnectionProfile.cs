namespace FirebirdAdmin.Application.Connections;

public sealed record ConnectionProfile(
    Guid Id,
    string Name,
    string Host,
    int Port,
    string Database,
    string UserName,
    string? Charset,
    string? Role,
    bool HasSavedPassword,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

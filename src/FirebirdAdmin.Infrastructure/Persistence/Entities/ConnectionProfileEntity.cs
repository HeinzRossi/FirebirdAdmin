namespace FirebirdAdmin.Infrastructure.Persistence.Entities;

public sealed class ConnectionProfileEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Database { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? Charset { get; set; }
    public string? Role { get; set; }
    public byte[]? ProtectedPasswordBlob { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

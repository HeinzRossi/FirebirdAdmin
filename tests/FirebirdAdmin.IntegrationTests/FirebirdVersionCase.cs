using FirebirdAdmin.Application.Connections;

namespace FirebirdAdmin.IntegrationTests;

internal sealed record FirebirdVersionCase(
    string Key,
    int ExpectedMajor,
    string Host,
    int Port,
    string Database,
    string User,
    string Password)
{
    public ConnectionProfile CreateProfile()
    {
        return new ConnectionProfile(
            Guid.NewGuid(),
            $"Integration {Key}",
            Host,
            Port,
            Database,
            User,
            "UTF8",
            null,
            HasSavedPassword: false,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);
    }
}

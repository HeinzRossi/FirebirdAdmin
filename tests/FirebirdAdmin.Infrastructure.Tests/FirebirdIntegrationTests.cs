using FirebirdAdmin.Application.Connections;
using FirebirdAdmin.Infrastructure.Connections;
using FluentAssertions;

namespace FirebirdAdmin.Infrastructure.Tests;

public sealed class FirebirdIntegrationTests
{
    [Fact]
    public async Task ConnectAsync_ShouldDetectVersion_WhenEnvironmentIsConfigured()
    {
        var env = FirebirdTestEnvironment.TryRead();
        if (env is null)
        {
            return;
        }

        var profile = new ConnectionProfile(
            Guid.NewGuid(),
            "Integration",
            env.Host,
            env.Port,
            env.Database,
            env.User,
            "UTF8",
            null,
            HasSavedPassword: false,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

        using var password = CredentialSecret.FromPlainText(env.Password);
        var service = new FirebirdConnectionService(
            new FirebirdCapabilitiesResolver(),
            new FirebirdToolsetDiscoveryService());

        var context = await service.ConnectAsync(new ConnectionRequest(profile, password), CancellationToken.None);

        context.ServerVersion.Major.Should().BeGreaterThan(0);
        context.Capabilities.Explanation.Should().NotBeNullOrWhiteSpace();
    }

    private sealed record FirebirdTestEnvironment(string Host, int Port, string Database, string User, string Password)
    {
        public static FirebirdTestEnvironment? TryRead()
        {
            var host = Environment.GetEnvironmentVariable("FIREBIRDADMIN_TEST_HOST");
            var database = Environment.GetEnvironmentVariable("FIREBIRDADMIN_TEST_DATABASE");
            var user = Environment.GetEnvironmentVariable("FIREBIRDADMIN_TEST_USER");
            var password = Environment.GetEnvironmentVariable("FIREBIRDADMIN_TEST_PASSWORD");

            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(database) ||
                string.IsNullOrWhiteSpace(user) ||
                string.IsNullOrWhiteSpace(password))
            {
                return null;
            }

            var port = int.TryParse(Environment.GetEnvironmentVariable("FIREBIRDADMIN_TEST_PORT"), out var parsedPort)
                ? parsedPort
                : 3050;

            return new FirebirdTestEnvironment(host, port, database, user, password);
        }
    }
}

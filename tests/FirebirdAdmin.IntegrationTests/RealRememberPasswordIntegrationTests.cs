using FirebirdAdmin.Application.Connections;
using FirebirdAdmin.Infrastructure.Connections;
using FirebirdAdmin.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using FluentAssertions;

namespace FirebirdAdmin.IntegrationTests;

public sealed class RealRememberPasswordIntegrationTests
{
    [Fact]
    public async Task RememberPassword_ShouldPersistAndReconnectToRealDatabase_WhenTestEnvironmentIsConfigured()
    {
        var environment = ReadEnvironment();
        if (environment is null)
        {
            return;
        }

        var databasePath = Path.Combine(
            Path.GetTempPath(),
            "FirebirdAdmin.RealRememberPassword",
            $"{Guid.NewGuid():N}.db");
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);

        try
        {
            var factory = new TestDbContextFactory(databasePath);
            var credentialStore = new DpapiCredentialStore(factory);
            var profileService = new ConnectionProfileService(factory, credentialStore);

            using (var password = CredentialSecret.FromPlainText(environment.Password))
            {
                var savedProfile = await profileService.SaveAsync(
                    new ConnectionProfileRequest(
                        null,
                        "RealRememberPassword",
                        environment.Host,
                        environment.Port,
                        environment.Database,
                        environment.User,
                        "UTF8",
                        null,
                        RememberPassword: true,
                        password),
                    CancellationToken.None);

                savedProfile.HasSavedPassword.Should().BeTrue();
            }

            var reopenedFactory = new TestDbContextFactory(databasePath);
            var reopenedCredentialStore = new DpapiCredentialStore(reopenedFactory);
            var reopenedProfileService = new ConnectionProfileService(reopenedFactory, reopenedCredentialStore);
            var reopenedProfile = (await reopenedProfileService.ListAsync(CancellationToken.None))
                .Single(profile => profile.Name == "RealRememberPassword");

            reopenedProfile.HasSavedPassword.Should().BeTrue();

            using var savedSecret = await reopenedCredentialStore.TryLoadAsync(reopenedProfile.Id, CancellationToken.None);
            savedSecret.Should().NotBeNull();
            savedSecret!.RevealAsString().Should().Be(environment.Password);

            var connectionService = new FirebirdConnectionService(
                new FirebirdCapabilitiesResolver(),
                new FirebirdToolsetDiscoveryService());

            using var connectionSecret = CredentialSecret.FromPlainText(savedSecret.RevealAsString());
            var context = await connectionService.ConnectAsync(
                new ConnectionRequest(reopenedProfile, connectionSecret),
                CancellationToken.None);

            context.ServerVersion.Major.Should().BeGreaterThan(0);
            context.Database.Should().Be(environment.Database);
        }
        finally
        {
            var directory = Path.GetDirectoryName(databasePath);
            if (directory is not null && Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    private static FirebirdTestEnvironment? ReadEnvironment()
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

    private sealed record FirebirdTestEnvironment(
        string Host,
        int Port,
        string Database,
        string User,
        string Password);

    private sealed class TestDbContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> options;

        public TestDbContextFactory(string databasePath)
        {
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = databasePath,
                Pooling = false,
                Mode = SqliteOpenMode.ReadWriteCreate
            }.ToString();

            options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connectionString)
                .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
                .Options;

            using var dbContext = new AppDbContext(options);
            dbContext.Database.Migrate();
        }

        public AppDbContext CreateDbContext()
        {
            return new AppDbContext(options);
        }
    }
}

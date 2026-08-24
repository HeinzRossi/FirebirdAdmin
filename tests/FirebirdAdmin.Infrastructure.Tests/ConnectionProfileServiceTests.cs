using FirebirdAdmin.Application.Connections;
using FirebirdAdmin.Infrastructure.Connections;
using FluentAssertions;

namespace FirebirdAdmin.Infrastructure.Tests;

public sealed class ConnectionProfileServiceTests
{
    [Fact]
    public async Task SaveAsync_ShouldPersistProfileWithoutPlainTextPassword()
    {
        using var fixture = TempDatabaseFixture.Create();
        var factory = new InfrastructureTestDbContextFactory(fixture.DatabasePath);
        var credentialStore = new DpapiCredentialStore(factory);
        var service = new ConnectionProfileService(factory, credentialStore);

        using var password = CredentialSecret.FromPlainText("masterkey");
        var profile = await service.SaveAsync(CreateRequest(password, rememberPassword: true), CancellationToken.None);

        profile.HasSavedPassword.Should().BeTrue();
        var loadedSecret = await credentialStore.TryLoadAsync(profile.Id, CancellationToken.None);
        loadedSecret.Should().NotBeNull();
        loadedSecret!.RevealAsString().Should().Be("masterkey");

        await using var dbContext = factory.CreateDbContext();
        var entity = dbContext.ConnectionProfiles.Single();
        entity.ProtectedPasswordBlob.Should().NotBeNullOrEmpty();
        entity.ProtectedPasswordBlob.Should().NotEqual("masterkey"u8.ToArray());
        entity.Name.Should().NotContain("masterkey");
        entity.Host.Should().NotContain("masterkey");
        entity.Database.Should().NotContain("masterkey");
        entity.UserName.Should().NotContain("masterkey");
    }

    [Fact]
    public async Task SaveAsync_ShouldNotPersistPasswordWhenRememberPasswordIsFalse()
    {
        using var fixture = TempDatabaseFixture.Create();
        var factory = new InfrastructureTestDbContextFactory(fixture.DatabasePath);
        var credentialStore = new DpapiCredentialStore(factory);
        var service = new ConnectionProfileService(factory, credentialStore);

        using var password = CredentialSecret.FromPlainText("masterkey");
        var profile = await service.SaveAsync(CreateRequest(password, rememberPassword: false), CancellationToken.None);

        profile.HasSavedPassword.Should().BeFalse();
        (await credentialStore.TryLoadAsync(profile.Id, CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task SaveAsync_ShouldUpdateExistingProfileWhenNameAlreadyExists()
    {
        using var fixture = TempDatabaseFixture.Create();
        var factory = new InfrastructureTestDbContextFactory(fixture.DatabasePath);
        var credentialStore = new DpapiCredentialStore(factory);
        var service = new ConnectionProfileService(factory, credentialStore);

        using var firstPassword = CredentialSecret.FromPlainText("masterkey");
        var first = await service.SaveAsync(CreateRequest(firstPassword, rememberPassword: false), CancellationToken.None);

        using var secondPassword = CredentialSecret.FromPlainText("masterkey");
        var second = await service.SaveAsync(
            CreateRequest(
                secondPassword,
                rememberPassword: false,
                host: "server-02",
                database: "prod.fdb",
                userName: "ADMIN"),
            CancellationToken.None);

        second.Id.Should().Be(first.Id);
        second.Host.Should().Be("server-02");
        second.Database.Should().Be("prod.fdb");
        second.UserName.Should().Be("ADMIN");
        second.CreatedAt.Should().Be(first.CreatedAt);
        second.UpdatedAt.Should().BeAfter(first.UpdatedAt);

        await using var dbContext = factory.CreateDbContext();
        dbContext.ConnectionProfiles.Should().ContainSingle();
    }

    [Fact]
    public async Task SaveAsync_ShouldClearSavedPasswordWhenUpdatingExistingProfileWithoutRememberPassword()
    {
        using var fixture = TempDatabaseFixture.Create();
        var factory = new InfrastructureTestDbContextFactory(fixture.DatabasePath);
        var credentialStore = new DpapiCredentialStore(factory);
        var service = new ConnectionProfileService(factory, credentialStore);

        using var savedPassword = CredentialSecret.FromPlainText("masterkey");
        var first = await service.SaveAsync(CreateRequest(savedPassword, rememberPassword: true), CancellationToken.None);
        first.HasSavedPassword.Should().BeTrue();

        using var transientPassword = CredentialSecret.FromPlainText("masterkey");
        var second = await service.SaveAsync(CreateRequest(transientPassword, rememberPassword: false), CancellationToken.None);

        second.Id.Should().Be(first.Id);
        second.HasSavedPassword.Should().BeFalse();

        await using var dbContext = factory.CreateDbContext();
        var entity = dbContext.ConnectionProfiles.Single();
        entity.ProtectedPasswordBlob.Should().BeNull();
    }

    private static ConnectionProfileRequest CreateRequest(
        CredentialSecret password,
        bool rememberPassword,
        string host = "localhost",
        string database = "employee.fdb",
        string userName = "SYSDBA")
    {
        return new ConnectionProfileRequest(
            Id: null,
            "Local",
            host,
            3050,
            database,
            userName,
            "UTF8",
            null,
            rememberPassword,
            password);
    }
}

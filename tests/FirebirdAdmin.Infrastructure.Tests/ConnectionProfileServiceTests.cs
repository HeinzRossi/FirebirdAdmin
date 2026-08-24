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

    private static ConnectionProfileRequest CreateRequest(CredentialSecret password, bool rememberPassword)
    {
        return new ConnectionProfileRequest(
            Id: null,
            "Local",
            "localhost",
            3050,
            "employee.fdb",
            "SYSDBA",
            "UTF8",
            null,
            rememberPassword,
            password);
    }
}

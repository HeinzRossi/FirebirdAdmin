using FirebirdAdmin.Infrastructure.Security;
using FluentAssertions;

namespace FirebirdAdmin.Infrastructure.Tests;

public sealed class SecretMaskerTests
{
    [Theory]
    [InlineData("User=SYSDBA;Password=masterkey;Database=db", "masterkey")]
    [InlineData("pwd=secret host=localhost", "secret")]
    [InlineData("gbak -user SYSDBA -password masterkey db backup", "masterkey")]
    public void MaskSecrets_ShouldRemoveKnownPasswordShapes(string input, string secret)
    {
        var masked = SecretMasker.MaskSecrets(input);

        masked.Should().NotContain(secret);
        masked.Should().Contain(SecretMasker.Mask);
    }
}

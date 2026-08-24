using System.Security.Cryptography;
using System.Text;
using FirebirdAdmin.Application.Connections;
using FirebirdAdmin.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FirebirdAdmin.Infrastructure.Connections;

public sealed class DpapiCredentialStore(IDbContextFactory<AppDbContext> dbContextFactory) : ICredentialStore
{
    public async Task SaveAsync(Guid profileId, CredentialSecret secret, CancellationToken cancellationToken)
    {
        ThrowIfUnsupported();
        var secretBytes = secret.CopyBytes();

        try
        {
            var protectedBytes = ProtectedData.Protect(
                secretBytes,
                GetEntropy(profileId),
                DataProtectionScope.CurrentUser);

            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            var profile = await dbContext.ConnectionProfiles.SingleAsync(entity => entity.Id == profileId, cancellationToken);
            profile.ProtectedPasswordBlob = protectedBytes;
            profile.UpdatedAt = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            Array.Clear(secretBytes);
        }
    }

    public async Task<CredentialSecret?> TryLoadAsync(Guid profileId, CancellationToken cancellationToken)
    {
        ThrowIfUnsupported();
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var protectedBytes = await dbContext.ConnectionProfiles
            .Where(entity => entity.Id == profileId)
            .Select(entity => entity.ProtectedPasswordBlob)
            .SingleOrDefaultAsync(cancellationToken);

        if (protectedBytes is null || protectedBytes.Length == 0)
        {
            return null;
        }

        try
        {
            var bytes = ProtectedData.Unprotect(
                protectedBytes,
                GetEntropy(profileId),
                DataProtectionScope.CurrentUser);

            return CredentialSecret.FromBytes(bytes);
        }
        catch (CryptographicException)
        {
            return null;
        }
    }

    public async Task DeleteAsync(Guid profileId, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var profile = await dbContext.ConnectionProfiles.SingleOrDefaultAsync(entity => entity.Id == profileId, cancellationToken);

        if (profile is null)
        {
            return;
        }

        profile.ProtectedPasswordBlob = null;
        profile.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static byte[] GetEntropy(Guid profileId)
    {
        return Encoding.UTF8.GetBytes($"FirebirdAdmin:ConnectionProfile:{profileId:N}");
    }

    private static void ThrowIfUnsupported()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("DPAPI credential storage requires Windows.");
        }
    }
}

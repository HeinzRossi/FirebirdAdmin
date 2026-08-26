using FirebirdAdmin.Application.Connections;
using FirebirdAdmin.Infrastructure.Persistence;
using FirebirdAdmin.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FirebirdAdmin.Infrastructure.Connections;

public sealed class ConnectionProfileService(
    IDbContextFactory<AppDbContext> dbContextFactory,
    ICredentialStore credentialStore) : IConnectionProfileService
{
    public async Task<IReadOnlyList<ConnectionProfile>> ListAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.ConnectionProfiles
            .OrderBy(entity => entity.Name)
            .Select(entity => ToModel(entity))
            .ToListAsync(cancellationToken);
    }

    public async Task<ConnectionProfile?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await dbContext.ConnectionProfiles.SingleOrDefaultAsync(profile => profile.Id == id, cancellationToken);
        return entity is null ? null : ToModel(entity);
    }

    public async Task<ConnectionProfile> SaveAsync(ConnectionProfileRequest request, CancellationToken cancellationToken)
    {
        Validate(request);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        ConnectionProfileEntity entity;

        var normalizedName = request.Name.Trim();

        if (request.Id is { } id)
        {
            entity = await dbContext.ConnectionProfiles.SingleOrDefaultAsync(profile => profile.Id == id, cancellationToken)
                ?? new ConnectionProfileEntity
                {
                    Id = id,
                    CreatedAt = now
                };

            if (dbContext.Entry(entity).State == EntityState.Detached)
            {
                dbContext.ConnectionProfiles.Add(entity);
            }
        }
        else
        {
            entity = await dbContext.ConnectionProfiles
                .SingleOrDefaultAsync(profile => profile.Name.ToUpper() == normalizedName.ToUpper(), cancellationToken)
                ?? new ConnectionProfileEntity
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = now
                };

            if (dbContext.Entry(entity).State == EntityState.Detached)
            {
                dbContext.ConnectionProfiles.Add(entity);
            }
        }

        entity.Name = normalizedName;
        entity.Host = request.Host.Trim();
        entity.Port = request.Port;
        entity.Database = request.Database.Trim();
        entity.UserName = request.UserName.Trim();
        entity.Charset = NormalizeOptional(request.Charset);
        entity.Role = NormalizeOptional(request.Role);
        entity.UpdatedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);

        if (request.RememberPassword && request.Password is not null)
        {
            await credentialStore.SaveAsync(entity.Id, request.Password, cancellationToken);
            await dbContext.Entry(entity).ReloadAsync(cancellationToken);
        }

        return ToModel(entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await dbContext.ConnectionProfiles.SingleOrDefaultAsync(profile => profile.Id == id, cancellationToken);

        if (entity is null)
        {
            return;
        }

        dbContext.ConnectionProfiles.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static ConnectionProfile ToModel(ConnectionProfileEntity entity)
    {
        return new ConnectionProfile(
            entity.Id,
            entity.Name,
            entity.Host,
            entity.Port,
            entity.Database,
            entity.UserName,
            entity.Charset,
            entity.Role,
            entity.ProtectedPasswordBlob is { Length: > 0 },
            entity.CreatedAt,
            entity.UpdatedAt);
    }

    private static void Validate(ConnectionProfileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.Host) ||
            string.IsNullOrWhiteSpace(request.Database) ||
            string.IsNullOrWhiteSpace(request.UserName))
        {
            throw new ArgumentException("Connection profile fields name, host, database and user are required.");
        }

        if (request.Port is <= 0 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Port must be between 1 and 65535.");
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

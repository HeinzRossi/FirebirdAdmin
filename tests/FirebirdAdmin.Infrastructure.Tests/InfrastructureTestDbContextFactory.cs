using FirebirdAdmin.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FirebirdAdmin.Infrastructure.Tests;

internal sealed class InfrastructureTestDbContextFactory : IDbContextFactory<AppDbContext>
{
    private readonly DbContextOptions<AppDbContext> options;

    public InfrastructureTestDbContextFactory(string databasePath)
    {
        options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={databasePath};Pooling=False")
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

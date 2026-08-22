using FirebirdAdmin.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FirebirdAdmin.Infrastructure.Tests;

internal sealed class InfrastructureTestDbContextFactory : IDbContextFactory<AppDbContext>
{
    private readonly DbContextOptions<AppDbContext> options;

    public InfrastructureTestDbContextFactory(string databasePath)
    {
        options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={databasePath};Pooling=False")
            .Options;

        using var dbContext = new AppDbContext(options);
        dbContext.Database.EnsureCreated();
    }

    public AppDbContext CreateDbContext()
    {
        return new AppDbContext(options);
    }
}

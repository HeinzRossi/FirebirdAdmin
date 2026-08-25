using FirebirdAdmin.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FirebirdAdmin.Infrastructure.Tests;

internal sealed class InfrastructureTestDbContextFactory : IDbContextFactory<AppDbContext>
{
    private readonly DbContextOptions<AppDbContext> options;

    public InfrastructureTestDbContextFactory(string databasePath, bool migrate = true)
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

        if (migrate)
        {
            using var dbContext = new AppDbContext(options);
            dbContext.Database.Migrate();
        }
    }

    public AppDbContext CreateDbContext()
    {
        return new AppDbContext(options);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace FirebirdAdmin.Infrastructure.Persistence;

public sealed class DatabaseInitializer(
    IDbContextFactory<AppDbContext> dbContextFactory,
    ApplicationDataPaths paths) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(paths.RootDirectory);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

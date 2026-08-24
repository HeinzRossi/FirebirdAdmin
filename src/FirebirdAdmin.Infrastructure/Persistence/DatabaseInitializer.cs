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
        BackupDatabaseIfPresent();

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await dbContext.Database.MigrateAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private void BackupDatabaseIfPresent()
    {
        if (!File.Exists(paths.DatabasePath))
        {
            return;
        }

        Directory.CreateDirectory(paths.BackupDirectory);
        var backupPath = Path.Combine(paths.BackupDirectory, $"firebird-admin-{DateTimeOffset.Now:yyyyMMdd-HHmmss}.db");
        File.Copy(paths.DatabasePath, backupPath, overwrite: false);

        foreach (var oldBackup in Directory
            .EnumerateFiles(paths.BackupDirectory, "firebird-admin-*.db")
            .OrderByDescending(File.GetCreationTimeUtc)
            .Skip(3))
        {
            File.Delete(oldBackup);
        }
    }
}

using Microsoft.Data.Sqlite;
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

        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            await dbContext.Database.MigrateAsync(cancellationToken);
        }
        catch (Exception ex) when (ShouldRecoverDatabaseFile(ex))
        {
            if (TryMoveDatabaseAside())
            {
                try
                {
                    await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
                    await dbContext.Database.MigrateAsync(cancellationToken);
                }
                catch (Exception retryException) when (ShouldRecoverDatabaseFile(retryException))
                {
                }
            }
        }
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
        var backupPath = Path.Combine(
            paths.BackupDirectory,
            $"firebird-admin-{DateTimeOffset.Now:yyyyMMdd-HHmmssfff}-{Guid.NewGuid():N}.db");

        try
        {
            File.Copy(paths.DatabasePath, backupPath, overwrite: false);

            foreach (var oldBackup in Directory
                .EnumerateFiles(paths.BackupDirectory, "firebird-admin-*.db")
                .OrderByDescending(File.GetCreationTimeUtc)
                .Skip(3))
            {
                File.Delete(oldBackup);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static bool ShouldRecoverDatabaseFile(Exception exception)
    {
        return exception is SqliteException { SqliteErrorCode: 14 or 26 } ||
               exception.Message.Contains("SQLite Error 14", StringComparison.OrdinalIgnoreCase) ||
               exception.Message.Contains("SQLite Error 26", StringComparison.OrdinalIgnoreCase) ||
               exception.Message.Contains("unable to open database file", StringComparison.OrdinalIgnoreCase) ||
               exception.Message.Contains("file is not a database", StringComparison.OrdinalIgnoreCase);
    }

    private bool TryMoveDatabaseAside()
    {
        if (!File.Exists(paths.DatabasePath))
        {
            return true;
        }

        try
        {
            Directory.CreateDirectory(paths.BackupDirectory);
            var recoveredPath = Path.Combine(
                paths.BackupDirectory,
                $"firebird-admin-recovered-{DateTimeOffset.Now:yyyyMMdd-HHmmssfff}-{Guid.NewGuid():N}.db");
            File.Move(paths.DatabasePath, recoveredPath, overwrite: false);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}

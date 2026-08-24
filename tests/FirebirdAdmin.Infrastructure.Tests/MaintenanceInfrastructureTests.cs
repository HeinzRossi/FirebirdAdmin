using FirebirdAdmin.Application.Connections;
using FirebirdAdmin.Application.Maintenance;
using FirebirdAdmin.Infrastructure.History;
using FirebirdAdmin.Infrastructure.Maintenance;
using FirebirdAdmin.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FirebirdAdmin.Infrastructure.Tests;

public sealed class MaintenanceInfrastructureTests
{
    [Fact]
    public void Migration_ShouldCreateMaintenanceTables()
    {
        using var fixture = TempDatabaseFixture.Create();
        var factory = new InfrastructureTestDbContextFactory(fixture.DatabasePath);
        using var dbContext = factory.CreateDbContext();

        var tables = dbContext.Database.SqlQueryRaw<string>(
            """
            SELECT name AS Value
            FROM sqlite_master
            WHERE type = 'table'
            ORDER BY name;
            """).ToArray();

        tables.Should().Contain("MaintenanceOperations");
        tables.Should().Contain("MaintenanceOperationLogs");
    }

    [Fact]
    public async Task Store_ShouldPersistOperationAndLogsWithMasking()
    {
        using var fixture = TempDirectoryFixture.Create();
        var paths = new ApplicationDataPaths(fixture.Path);
        var factory = new InfrastructureTestDbContextFactory(paths.DatabasePath);
        var store = new SqliteMaintenanceHistoryStore(new SqliteConnectionFactory(paths));
        var operation = new MaintenanceOperation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            MaintenanceOperationType.Backup,
            MaintenanceOperationStatus.Succeeded,
            "db.fdb password=masterkey",
            "db.fbk",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            0,
            "ok password=masterkey");

        await store.SaveOperationAsync(operation, CancellationToken.None);
        await store.SaveLogAsync(new MaintenanceLogLine(operation.Id, DateTimeOffset.UtcNow, "stdout", "-password masterkey"), CancellationToken.None);

        var recent = await store.ListRecentAsync(10, CancellationToken.None);

        recent.Should().ContainSingle();
        recent[0].Source.Should().NotContain("masterkey");
        recent[0].Message.Should().NotContain("masterkey");
    }

    [Fact]
    public async Task Runner_ShouldMaskCommandLineAndOutput()
    {
        var runner = new FirebirdToolRunner();
        var logs = new List<MaintenanceLogLine>();
        var powershell = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe");
        var command = new FirebirdToolCommand(
            FirebirdToolKind.Backup,
            powershell,
            ["-NoProfile", "-Command", "Write-Output 'password=masterkey'", "password=masterkey"],
            Environment.CurrentDirectory,
            new Dictionary<string, string> { ["ISC_PASSWORD"] = "masterkey" });

        var result = await runner.ExecuteAsync(Guid.NewGuid(), command, new Progress<MaintenanceLogLine>(logs.Add), CancellationToken.None);

        result.ExitCode.Should().Be(0);
        logs.Should().NotBeEmpty();
        logs.Select(log => log.Text).Should().NotContain(text => text.Contains("masterkey", StringComparison.OrdinalIgnoreCase));
    }
}

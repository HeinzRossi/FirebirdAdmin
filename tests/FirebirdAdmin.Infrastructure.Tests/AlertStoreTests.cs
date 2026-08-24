using Dapper;
using FirebirdAdmin.Application.Diagnostics;
using FirebirdAdmin.Infrastructure.Diagnostics;
using FirebirdAdmin.Infrastructure.History;
using FirebirdAdmin.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FirebirdAdmin.Infrastructure.Tests;

public sealed class AlertStoreTests
{
    [Fact]
    public async Task Migration_ShouldCreateAlertTables()
    {
        using var fixture = TempDatabaseFixture.Create();
        var factory = new InfrastructureTestDbContextFactory(fixture.DatabasePath);

        await using var dbContext = factory.CreateDbContext();
        var tables = await dbContext.Database.SqlQueryRaw<string>(
            "SELECT name AS Value FROM sqlite_master WHERE type = 'table' ORDER BY name;").ToArrayAsync();

        tables.Should().Contain(["AlertEvents", "AlertNotifications"]);
    }

    [Fact]
    public async Task UpsertAsync_ShouldDeduplicateAndIncrementOccurrences()
    {
        using var root = CreateInitializedRoot(out var paths);
        var store = new SqliteAlertStore(new SqliteConnectionFactory(paths), new AlertCorrelator());
        var result = CreateResult("password=masterkey");

        var first = await store.UpsertAsync(result, CancellationToken.None);
        var second = await store.UpsertAsync(result with { ObservedAt = result.ObservedAt.AddSeconds(1) }, CancellationToken.None);

        second.Id.Should().Be(first.Id);
        second.Occurrences.Should().Be(2);
        second.Message.Should().NotContain("masterkey");
    }

    [Fact]
    public async Task SetStatusAsync_ShouldUpdateLifecycle()
    {
        using var root = CreateInitializedRoot(out var paths);
        var store = new SqliteAlertStore(new SqliteConnectionFactory(paths), new AlertCorrelator());
        var alert = await store.UpsertAsync(CreateResult("msg"), CancellationToken.None);

        await store.SetStatusAsync(alert.Id, AlertStatus.Acknowledged, "visto", CancellationToken.None);

        var loaded = (await store.ListAsync(AlertStatus.Acknowledged, null, CancellationToken.None)).Single();
        loaded.Status.Should().Be(AlertStatus.Acknowledged);
        loaded.AcknowledgementNote.Should().Be("visto");
    }

    [Fact]
    public async Task InAppNotificationChannel_ShouldCollectAlerts()
    {
        var channel = new InAppNotificationChannel();
        var alert = new Alert(Guid.NewGuid(), "R", "K", DiagnosticSeverity.Low, AlertStatus.Active, "msg", new DiagnosticTarget("T", "1"), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 1, []);

        await channel.NotifyAsync(alert, CancellationToken.None);

        channel.Notifications.Should().ContainSingle();
    }

    private static DiagnosticResult CreateResult(string message)
    {
        return new DiagnosticResult(
            "RULE",
            DiagnosticSeverity.High,
            message,
            new DiagnosticTarget("Database", "1", "db"),
            DateTimeOffset.UtcNow,
            Guid.NewGuid(),
            Guid.NewGuid(),
            [new DiagnosticEvidence("Sql", "select 1 -- password=masterkey")]);
    }

    private static TempDirectoryFixture CreateInitializedRoot(out ApplicationDataPaths paths)
    {
        var root = TempDirectoryFixture.Create();
        paths = new ApplicationDataPaths(root.Path);
        _ = new InfrastructureTestDbContextFactory(paths.DatabasePath);
        return root;
    }
}

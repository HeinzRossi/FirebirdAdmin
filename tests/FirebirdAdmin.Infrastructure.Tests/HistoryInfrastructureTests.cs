using Dapper;
using FirebirdAdmin.Application.History;
using FirebirdAdmin.Application.Monitoring;
using FirebirdAdmin.Application.Profiler;
using FirebirdAdmin.Infrastructure.History;
using FirebirdAdmin.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace FirebirdAdmin.Infrastructure.Tests;

public sealed class HistoryInfrastructureTests
{
    [Fact]
    public async Task Migration_ShouldCreateHistorySchema()
    {
        using var fixture = TempDatabaseFixture.Create();
        var factory = new InfrastructureTestDbContextFactory(fixture.DatabasePath);

        await using var dbContext = factory.CreateDbContext();
        var tables = await dbContext.Database.SqlQueryRaw<string>(
            "SELECT name AS Value FROM sqlite_master WHERE type = 'table' ORDER BY name;").ToArrayAsync();

        tables.Should().Contain(["TraceEvents", "MonitoringSnapshots", "HistoryRetentionPolicies"]);
    }

    [Fact]
    public async Task DatabaseInitializer_ShouldBackupExistingDatabaseAndRetainThreeCopies()
    {
        using var root = TempDirectoryFixture.Create();
        var paths = new ApplicationDataPaths(root.Path);
        Directory.CreateDirectory(paths.RootDirectory);
        await using (var legacyConnection = new SqliteConnection($"Data Source={paths.DatabasePath};Pooling=False"))
        {
            await legacyConnection.OpenAsync();
            await legacyConnection.ExecuteAsync("""
                CREATE TABLE ConnectionProfiles (
                    Id TEXT NOT NULL PRIMARY KEY,
                    Name TEXT NOT NULL,
                    Host TEXT NOT NULL,
                    Port INTEGER NOT NULL,
                    Database TEXT NOT NULL,
                    UserName TEXT NOT NULL,
                    Charset TEXT NULL,
                    Role TEXT NULL,
                    ProtectedPasswordBlob BLOB NULL,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL
                );
                """);
        }

        for (var index = 0; index < 4; index++)
        {
            await Task.Delay(1100);
            var initializer = new DatabaseInitializer(new InfrastructureTestDbContextFactory(paths.DatabasePath), paths);
            await initializer.StartAsync(CancellationToken.None);
        }

        Directory.EnumerateFiles(paths.BackupDirectory, "firebird-admin-*.db").Should().HaveCount(3);
    }

    [Fact]
    public async Task DatabaseInitializer_ShouldRecoverInvalidDatabaseFile()
    {
        using var root = TempDirectoryFixture.Create();
        var paths = new ApplicationDataPaths(root.Path);
        Directory.CreateDirectory(paths.RootDirectory);
        await File.WriteAllTextAsync(paths.DatabasePath, "not a sqlite database");

        var initializer = new DatabaseInitializer(new InfrastructureTestDbContextFactory(paths.DatabasePath, migrate: false), paths);

        await initializer.StartAsync(CancellationToken.None);

        Directory.EnumerateFiles(paths.BackupDirectory, "firebird-admin-recovered-*.db").Should().ContainSingle();
        await using var dbContext = new InfrastructureTestDbContextFactory(paths.DatabasePath, migrate: false).CreateDbContext();
        var tables = await dbContext.Database.SqlQueryRaw<string>(
            "SELECT name AS Value FROM sqlite_master WHERE type = 'table' ORDER BY name;").ToArrayAsync();
        tables.Should().Contain("ConnectionProfiles");
    }

    [Fact]
    public async Task WriterAndQuery_ShouldPersistAndFilterTraceEvents()
    {
        using var root = CreateInitializedRoot(out var paths);
        var writer = new DapperHistoryWriter(new SqliteConnectionFactory(paths));
        var query = new DapperHistoryQueryService(new SqliteConnectionFactory(paths));

        await writer.WriteProfilerEventsAsync(
            Guid.NewGuid(),
            [
                CreateTraceEvent(1, "SYSDBA", "select * from customers", 12, 34, TimeSpan.FromMilliseconds(20)),
                CreateTraceEvent(2, "APP", "update orders set id = id", 13, 35, TimeSpan.FromMilliseconds(1))
            ],
            CancellationToken.None);

        var page = await query.QueryTraceEventsAsync(new HistoryQuery(
            SqlText: "customers",
            UserName: "SYSDBA",
            AttachmentId: 12,
            TransactionId: 34,
            MinimumDuration: TimeSpan.FromMilliseconds(10)),
            CancellationToken.None);

        page.TotalCount.Should().Be(1);
        page.Items.Should().ContainSingle(item => item.Sequence == 1);
    }

    [Fact]
    public async Task BufferedWriter_ShouldFlushPendingEventsWhenStopped()
    {
        using var root = CreateInitializedRoot(out var paths);
        var connectionFactory = new SqliteConnectionFactory(paths);
        var writer = new BufferedHistoryWriter(
            new DapperHistoryWriter(connectionFactory),
            NullLogger<BufferedHistoryWriter>.Instance);

        await writer.StartAsync(CancellationToken.None);
        await writer.WriteProfilerEventsAsync(
            null,
            [CreateTraceEvent(1, "SYSDBA", "select 1", null, null, null)],
            CancellationToken.None);
        await writer.StopAsync(CancellationToken.None);

        await using var connection = connectionFactory.Create();
        await connection.OpenAsync();
        var count = await connection.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM TraceEvents;");
        count.Should().Be(1);
    }

    [Fact]
    public async Task WriterAndQuery_ShouldPersistMonitoringSnapshots()
    {
        using var root = CreateInitializedRoot(out var paths);
        var writer = new DapperHistoryWriter(new SqliteConnectionFactory(paths));
        var query = new DapperHistoryQueryService(new SqliteConnectionFactory(paths));

        var snapshot = new MonitoringSnapshot(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            [new AttachmentSnapshot(1, "SYSDBA", "127.0.0.1", "app", DateTimeOffset.UtcNow, "1")],
            [new TransactionSnapshot(2, 1, "1", DateTimeOffset.UtcNow, 1, 2, 3, 4)],
            [new StatementSnapshot(3, 1, 2, "1", DateTimeOffset.UtcNow, "select 1")]);

        await writer.WriteMonitoringSnapshotsAsync(Guid.NewGuid(), [snapshot], CancellationToken.None);

        var page = await query.QueryMonitoringSnapshotsAsync(new HistoryQuery(Kind: HistoryDataKind.MonitoringSnapshots), CancellationToken.None);

        page.TotalCount.Should().Be(1);
        page.Items[0].AttachmentCount.Should().Be(1);
        page.Items[0].TransactionCount.Should().Be(1);
        page.Items[0].StatementCount.Should().Be(1);
    }

    [Fact]
    public async Task Retention_ShouldRemoveExpiredRows()
    {
        using var root = CreateInitializedRoot(out var paths);
        var connectionFactory = new SqliteConnectionFactory(paths);
        var writer = new DapperHistoryWriter(connectionFactory);
        var retention = new SqliteRetentionPolicyService(connectionFactory, paths);

        await using var connection = connectionFactory.Create();
        await connection.OpenAsync();
        await connection.ExecuteAsync("UPDATE HistoryRetentionPolicies SET RetentionDays = 1, BatchSize = 500 WHERE Id = 1;");

        await writer.WriteProfilerEventsAsync(null, [CreateTraceEvent(1, "SYSDBA", "select 1", null, null, null, DateTimeOffset.UtcNow.AddDays(-3))], CancellationToken.None);

        await retention.ApplyRetentionAsync(CancellationToken.None);

        var remaining = await connection.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM TraceEvents;");
        remaining.Should().Be(0);
    }

    [Fact]
    public async Task RetentionHostedService_ShouldStopWithoutTaskCanceledExceptionDuringDelay()
    {
        var retention = new ControllableRetentionPolicyService();
        var service = new HistoryRetentionHostedService(
            retention,
            NullLogger<HistoryRetentionHostedService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await retention.WaitForApplyAsync();

        var act = async () => await service.StopAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RetentionHostedService_ShouldStopWithoutTaskCanceledExceptionDuringRetention()
    {
        var retention = new BlockingRetentionPolicyService();
        var service = new HistoryRetentionHostedService(
            retention,
            NullLogger<HistoryRetentionHostedService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await retention.WaitForApplyAsync();

        var act = async () => await service.StopAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Export_ShouldCreateCsvAndJsonWithoutSecrets()
    {
        using var root = CreateInitializedRoot(out var paths);
        var connectionFactory = new SqliteConnectionFactory(paths);
        var writer = new DapperHistoryWriter(connectionFactory);
        var query = new DapperHistoryQueryService(connectionFactory);
        var export = new HistoryExportService(query, paths);

        await writer.WriteProfilerEventsAsync(null, [CreateTraceEvent(1, "SYSDBA", "select 1 -- password=masterkey", null, null, null)], CancellationToken.None);

        var csv = await export.ExportAsync(new ExportRequest(new HistoryQuery(SqlText: "select"), ExportFormat.Csv), CancellationToken.None);
        var json = await export.ExportAsync(new ExportRequest(new HistoryQuery(SqlText: "select"), ExportFormat.Json), CancellationToken.None);

        File.Exists(csv.OutputPath).Should().BeTrue();
        File.Exists(json.OutputPath).Should().BeTrue();
        (await File.ReadAllTextAsync(csv.OutputPath)).Should().NotContain("masterkey");
        (await File.ReadAllTextAsync(json.OutputPath)).Should().NotContain("masterkey");
    }

    [Fact]
    public async Task Export_ShouldIncludeEveryPageAndNeutralizeCsvFormulas()
    {
        using var root = CreateInitializedRoot(out var paths);
        var connectionFactory = new SqliteConnectionFactory(paths);
        var writer = new DapperHistoryWriter(connectionFactory);
        var query = new DapperHistoryQueryService(connectionFactory);
        var export = new HistoryExportService(query, paths);
        var events = Enumerable.Range(1, 501)
            .Select(sequence => CreateTraceEvent(sequence, "=HYPERLINK(\"https://example.invalid\")", $"select {sequence}", null, null, null))
            .ToArray();

        await writer.WriteProfilerEventsAsync(null, events, CancellationToken.None);

        var result = await export.ExportAsync(
            new ExportRequest(new HistoryQuery(), ExportFormat.Csv),
            CancellationToken.None);

        result.RowCount.Should().Be(501);
        var contents = await File.ReadAllTextAsync(result.OutputPath);
        contents.Should().Contain("'=HYPERLINK");
    }

    private static ProfilerEvent CreateTraceEvent(
        long sequence,
        string userName,
        string sql,
        long? attachmentId,
        long? transactionId,
        TimeSpan? duration,
        DateTimeOffset? timestamp = null)
    {
        return new ProfilerEvent(
            sequence,
            timestamp ?? DateTimeOffset.UtcNow,
            TraceEventType.StatementFinished,
            duration,
            userName,
            attachmentId,
            transactionId,
            sql,
            new ProfilerMetrics(1, 2, 3, 4),
            "plan natural",
            sql);
    }

    private static TempDirectoryFixture CreateInitializedRoot(out ApplicationDataPaths paths)
    {
        var root = TempDirectoryFixture.Create();
        paths = new ApplicationDataPaths(root.Path);
        _ = new InfrastructureTestDbContextFactory(paths.DatabasePath);
        return root;
    }

    private sealed class ControllableRetentionPolicyService : IRetentionPolicyService
    {
        private readonly TaskCompletionSource applied = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<RetentionPolicy> GetPolicyAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(new RetentionPolicy());
        }

        public Task ApplyRetentionAsync(CancellationToken cancellationToken)
        {
            applied.TrySetResult();
            return Task.CompletedTask;
        }

        public Task WaitForApplyAsync()
        {
            return applied.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }

    private sealed class BlockingRetentionPolicyService : IRetentionPolicyService
    {
        private readonly TaskCompletionSource applied = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<RetentionPolicy> GetPolicyAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(new RetentionPolicy());
        }

        public async Task ApplyRetentionAsync(CancellationToken cancellationToken)
        {
            applied.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }

        public Task WaitForApplyAsync()
        {
            return applied.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }
}

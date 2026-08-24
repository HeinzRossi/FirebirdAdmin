using FirebirdAdmin.Application.Connections;
using FirebirdAdmin.Application.Maintenance;
using FluentAssertions;

namespace FirebirdAdmin.Application.Tests;

public sealed class MaintenanceTests
{
    [Fact]
    public async Task Preflight_ShouldBlockMissingTool()
    {
        var service = new MaintenancePreflightService();
        var request = new BackupRequest(CreateConnection(EffectiveToolset.Empty), "db.fdb", "db.fbk", Confirmed: true);

        var result = await service.ValidateAsync(request, CancellationToken.None);

        result.CanExecute.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Contains("gbak", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Preflight_ShouldBlockRestoreOverwrite()
    {
        var restorePath = Path.GetTempFileName();
        try
        {
            var service = new MaintenancePreflightService();
            var request = new RestoreRequest(CreateConnection(CreateToolset()), "backup.fbk", restorePath, Confirmed: true);

            var result = await service.ValidateAsync(request, CancellationToken.None);

            result.CanExecute.Should().BeFalse();
            result.Errors.Should().Contain(error => error.Contains("overwrite", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            File.Delete(restorePath);
        }
    }

    [Fact]
    public async Task MaintenanceService_ShouldRejectConcurrentOperation()
    {
        var runner = new BlockingRunner();
        var service = new MaintenanceService(new AlwaysValidPreflight(), runner, new InMemoryMaintenanceHistoryStore());
        var request = new SweepRequest(CreateConnection(CreateToolset()), "db.fdb", Confirmed: true);
        using var firstCts = new CancellationTokenSource();

        var first = service.ExecuteAsync(request, null, firstCts.Token);
        await runner.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var act = () => service.ExecuteAsync(request, null, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*operação de manutenção*");
        firstCts.Cancel();
        await first;
    }

    [Fact]
    public async Task MaintenanceService_ShouldReturnCancelledWhenTokenCancels()
    {
        var runner = new BlockingRunner();
        var service = new MaintenanceService(new AlwaysValidPreflight(), runner, new InMemoryMaintenanceHistoryStore());
        using var cts = new CancellationTokenSource();
        var task = service.ExecuteAsync(new SweepRequest(CreateConnection(CreateToolset()), "db.fdb", Confirmed: true), null, cts.Token);

        await runner.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));
        cts.Cancel();
        var result = await task;

        result.Operation.Status.Should().Be(MaintenanceOperationStatus.Cancelled);
    }

    private static ConnectionContext CreateConnection(EffectiveToolset toolset)
    {
        return new ConnectionContext(
            Guid.NewGuid(),
            "Local",
            "localhost",
            3050,
            "db.fdb",
            "SYSDBA",
            FirebirdServerVersion.Parse("5.0.0"),
            new FirebirdCapabilities(true, true, true, true, true, "test"),
            toolset,
            DateTimeOffset.UtcNow);
    }

    private static EffectiveToolset CreateToolset()
    {
        return new EffectiveToolset(
            [
                new ToolsetCandidate(FirebirdToolKind.Backup, "gbak.exe", null, true),
                new ToolsetCandidate(FirebirdToolKind.Fix, "gfix.exe", null, true)
            ]);
    }

    private sealed class AlwaysValidPreflight : IMaintenancePreflightService
    {
        public Task<MaintenancePreflightResult> ValidateAsync(MaintenanceRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new MaintenancePreflightResult(true, [], [], []));
        }
    }

    private sealed class BlockingRunner : IFirebirdToolRunner
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<ToolExecutionResult> ExecuteAsync(Guid operationId, FirebirdToolCommand command, IProgress<MaintenanceLogLine> progress, CancellationToken cancellationToken)
        {
            Started.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new ToolExecutionResult(0, []);
        }
    }

    private sealed class InMemoryMaintenanceHistoryStore : IMaintenanceHistoryStore
    {
        public Task SaveOperationAsync(MaintenanceOperation operation, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SaveLogAsync(MaintenanceLogLine logLine, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IReadOnlyList<MaintenanceOperation>> ListRecentAsync(int take, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<MaintenanceOperation>>([]);
    }
}

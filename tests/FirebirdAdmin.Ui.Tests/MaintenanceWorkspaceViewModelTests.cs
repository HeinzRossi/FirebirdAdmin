using FirebirdAdmin.Application.Connections;
using FirebirdAdmin.Application.Maintenance;
using FirebirdAdmin.Presentation.Wpf.Maintenance;
using FirebirdAdmin.Presentation.Wpf.Resources;
using FluentAssertions;

namespace FirebirdAdmin.Ui.Tests;

public sealed class MaintenanceWorkspaceViewModelTests
{
    [Fact]
    public void Constructor_ShouldStartDisconnected()
    {
        var viewModel = new MaintenanceWorkspaceViewModel(new FakeMaintenanceService(), new FakeMaintenanceHistoryStore());

        viewModel.CanExecute.Should().BeFalse();
        viewModel.Message.Should().Contain("Conecte");
        viewModel.IsProgressVisible.Should().BeTrue();
        viewModel.IsProgressIndeterminate.Should().BeFalse();
        viewModel.ProgressValue.Should().Be(0);
        viewModel.ProgressStatusText.Should().Be(AppStrings.MaintenanceProgressWaiting);
    }

    [Fact]
    public async Task ValidateAsync_ShouldEnableExecutionWhenConfirmed()
    {
        var viewModel = new MaintenanceWorkspaceViewModel(new FakeMaintenanceService(), new FakeMaintenanceHistoryStore());
        viewModel.SetConnection(CreateConnection(), null);
        viewModel.Confirmed = true;

        await viewModel.ValidateAsync();

        viewModel.PreflightText.Should().Contain("REVISÃO");
        viewModel.CanExecute.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUpdateStateAndHistory()
    {
        var viewModel = new MaintenanceWorkspaceViewModel(new FakeMaintenanceService(), new FakeMaintenanceHistoryStore());
        viewModel.SetConnection(CreateConnection(), null);
        viewModel.Confirmed = true;

        await viewModel.ExecuteAsync();

        viewModel.IsRunning.Should().BeFalse();
        viewModel.Logs.Should().ContainSingle();
        viewModel.History.Should().ContainSingle();
        viewModel.IsProgressIndeterminate.Should().BeFalse();
        viewModel.ProgressValue.Should().Be(100);
        viewModel.ProgressStatusText.Should().Be(AppStrings.MaintenanceProgressCompleted);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldShowIndeterminateProgressWhileOperationRuns()
    {
        var service = new BlockingMaintenanceService();
        var viewModel = new MaintenanceWorkspaceViewModel(service, new FakeMaintenanceHistoryStore());
        viewModel.SetConnection(CreateConnection(), null);
        viewModel.Confirmed = true;

        var execution = viewModel.ExecuteAsync();
        await service.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));

        viewModel.IsRunning.Should().BeTrue();
        viewModel.IsProgressVisible.Should().BeTrue();
        viewModel.IsProgressIndeterminate.Should().BeTrue();
        viewModel.ProgressStatusText.Should().Contain("processando");

        service.Complete();
        await execution;
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUseDeterminateProgress_WhenPercentIsAvailable()
    {
        var service = new FakeMaintenanceService(progressPercent: 0.5);
        var viewModel = new MaintenanceWorkspaceViewModel(service, new FakeMaintenanceHistoryStore());
        viewModel.SetConnection(CreateConnection(), null);
        viewModel.Confirmed = true;

        await viewModel.ExecuteAsync();

        service.ProgressValues.Should().Contain(50);
        viewModel.IsProgressIndeterminate.Should().BeFalse();
        viewModel.ProgressValue.Should().Be(100);
        viewModel.ProgressStatusText.Should().Be(AppStrings.MaintenanceProgressCompleted);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldStopProgress_WhenOperationFailsOrCancels()
    {
        var service = new FakeMaintenanceService(finalStatus: MaintenanceOperationStatus.Failed);
        var viewModel = new MaintenanceWorkspaceViewModel(service, new FakeMaintenanceHistoryStore());
        viewModel.SetConnection(CreateConnection(), null);
        viewModel.Confirmed = true;

        await viewModel.ExecuteAsync();

        viewModel.IsProgressIndeterminate.Should().BeFalse();
        viewModel.ProgressStatusText.Should().Be(AppStrings.MaintenanceProgressFailed);
    }

    private static ConnectionContext CreateConnection()
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
            new EffectiveToolset([new ToolsetCandidate(FirebirdToolKind.Backup, "gbak.exe", null, true), new ToolsetCandidate(FirebirdToolKind.Fix, "gfix.exe", null, true)]),
            DateTimeOffset.UtcNow);
    }

    private sealed class FakeMaintenanceService(
        MaintenanceOperationStatus finalStatus = MaintenanceOperationStatus.Succeeded,
        double? progressPercent = 1) : IMaintenanceService
    {
        public MaintenanceOperation? ActiveOperation => null;
        public event EventHandler<MaintenanceProgress>? ProgressChanged;
        public event EventHandler<MaintenanceLogLine>? LogReceived;
        public List<double> ProgressValues { get; } = [];

        public Task<MaintenancePreflightResult> ValidateAsync(MaintenanceRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new MaintenancePreflightResult(true, [], [], ["ok"]));
        }

        public Task<MaintenanceResult> ExecuteAsync(MaintenanceRequest request, CredentialSecret? password, CancellationToken cancellationToken)
        {
            var operation = new MaintenanceOperation(Guid.NewGuid(), request.Connection.ProfileId, request.Type, finalStatus, request.Source, request.Target, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, finalStatus is MaintenanceOperationStatus.Succeeded ? 0 : 1, "ok");
            ProgressChanged?.Invoke(this, new MaintenanceProgress(operation.Id, "Resultado", progressPercent, "ok", DateTimeOffset.UtcNow));
            if (progressPercent is not null)
            {
                ProgressValues.Add(progressPercent.Value * 100);
            }

            LogReceived?.Invoke(this, new MaintenanceLogLine(operation.Id, DateTimeOffset.UtcNow, "stdout", "ok"));
            return Task.FromResult(new MaintenanceResult(operation, []));
        }
    }

    private sealed class BlockingMaintenanceService : IMaintenanceService
    {
        private readonly TaskCompletionSource complete = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public MaintenanceOperation? ActiveOperation => null;
        public event EventHandler<MaintenanceProgress>? ProgressChanged;
        public event EventHandler<MaintenanceLogLine>? LogReceived;

        public Task<MaintenancePreflightResult> ValidateAsync(MaintenanceRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new MaintenancePreflightResult(true, [], [], ["ok"]));
        }

        public async Task<MaintenanceResult> ExecuteAsync(MaintenanceRequest request, CredentialSecret? password, CancellationToken cancellationToken)
        {
            var operation = new MaintenanceOperation(Guid.NewGuid(), request.Connection.ProfileId, request.Type, MaintenanceOperationStatus.Succeeded, request.Source, request.Target, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 0, "ok");
            ProgressChanged?.Invoke(this, new MaintenanceProgress(operation.Id, "Executando", null, "processando", DateTimeOffset.UtcNow));
            LogReceived?.Invoke(this, new MaintenanceLogLine(operation.Id, DateTimeOffset.UtcNow, "stdout", "processando"));
            Started.TrySetResult();
            await complete.Task.WaitAsync(cancellationToken);
            ProgressChanged?.Invoke(this, new MaintenanceProgress(operation.Id, "Resultado", 1, "ok", DateTimeOffset.UtcNow));
            return new MaintenanceResult(operation, []);
        }

        public void Complete()
        {
            complete.TrySetResult();
        }
    }

    private sealed class FakeMaintenanceHistoryStore : IMaintenanceHistoryStore
    {
        private readonly List<MaintenanceOperation> operations = [];

        public Task SaveOperationAsync(MaintenanceOperation operation, CancellationToken cancellationToken)
        {
            operations.Add(operation);
            return Task.CompletedTask;
        }

        public Task SaveLogAsync(MaintenanceLogLine logLine, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IReadOnlyList<MaintenanceOperation>> ListRecentAsync(int take, CancellationToken cancellationToken)
        {
            if (operations.Count == 0)
            {
                operations.Add(new MaintenanceOperation(Guid.NewGuid(), Guid.NewGuid(), MaintenanceOperationType.Backup, MaintenanceOperationStatus.Succeeded, "db.fdb", "db.fbk", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 0, "ok"));
            }

            return Task.FromResult<IReadOnlyList<MaintenanceOperation>>(operations.Take(take).ToArray());
        }
    }
}

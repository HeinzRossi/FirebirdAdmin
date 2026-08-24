using FirebirdAdmin.Application.Connections;
using FirebirdAdmin.Application.Dashboard;
using FirebirdAdmin.Application.History;
using FirebirdAdmin.Application.Monitoring;
using FirebirdAdmin.Application.Profiler;
using FirebirdAdmin.Presentation.Wpf.Dashboard;
using FirebirdAdmin.Presentation.Wpf.History;
using FirebirdAdmin.Presentation.Wpf.Monitoring;
using FirebirdAdmin.Presentation.Wpf.Profiler;
using FirebirdAdmin.Presentation.Wpf.Resources;
using FirebirdAdmin.Presentation.Wpf.Shell;
using FluentAssertions;

namespace FirebirdAdmin.Ui.Tests;

public sealed class ShellViewModelTests
{
    [Fact]
    public void Constructor_ShouldExposeSprintOneInitialState()
    {
        var viewModel = CreateViewModel();

        viewModel.IsNavigationExpanded.Should().BeTrue();
        viewModel.HasActiveConnection.Should().BeFalse();
        viewModel.IsTraceRunning.Should().BeFalse();
        viewModel.IsPollingRunning.Should().BeFalse();
        viewModel.ConnectionState.Should().Be(ShellConnectionState.Disconnected);
        viewModel.ReadyStatus.Should().Be(AppStrings.ReadyStatus);
        viewModel.TraceStatus.Should().Be(AppStrings.TraceStopped);
        viewModel.PollingStatus.Should().Be(AppStrings.PollingStopped);
        viewModel.Port.Should().Be(3050);
        viewModel.UserName.Should().Be("SYSDBA");
    }

    [Fact]
    public async Task ConnectAsync_ShouldSetConnectedStateAndContext()
    {
        var viewModel = CreateViewModel();
        viewModel.Database = "employee.fdb";

        await viewModel.ConnectAsync("masterkey");

        viewModel.ConnectionState.Should().Be(ShellConnectionState.Connected);
        viewModel.HasActiveConnection.Should().BeTrue();
        viewModel.ConnectionContext.Should().Contain("Firebird");
        await WaitUntilAsync(() => viewModel.TransactionsWorkspace.State == TransactionsWorkspaceState.Ready);
        viewModel.Dashboard.Health.Should().Be(DatabaseHealthStatus.Healthy);
        viewModel.Dashboard.Metrics.Should().Contain(metric => metric.Key == "transactions" && metric.Value == "1");
    }

    [Fact]
    public async Task ConnectAsync_ShouldSetFailedStateWhenConnectionFails()
    {
        var viewModel = CreateViewModel(connectionShouldFail: true);
        viewModel.Database = "employee.fdb";

        await viewModel.ConnectAsync("masterkey");

        viewModel.ConnectionState.Should().Be(ShellConnectionState.ConnectionFailed);
        viewModel.HasActiveConnection.Should().BeFalse();
    }

    private static ShellViewModel CreateViewModel(bool connectionShouldFail = false)
    {
        return new ShellViewModel(
            new FakeConnectionProfileService(),
            new FakeCredentialStore(),
            new FakeFirebirdConnectionService(connectionShouldFail),
            new FakeMonitoringSessionService(),
            new FakeHistoryWriter(),
            new TransactionsWorkspaceViewModel(),
            new DashboardViewModel(new DashboardProjectionService()),
            new ProfilerWorkspaceViewModel(new FakeProfilerSessionService(), new FakeHistoryWriter()),
            new HistoryWorkspaceViewModel(new FakeHistoryQueryService(), new FakeHistoryExportService()));
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        while (!condition())
        {
            await Task.Delay(20, timeout.Token);
        }
    }

    private sealed class FakeConnectionProfileService : IConnectionProfileService
    {
        public Task<IReadOnlyList<ConnectionProfile>> ListAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<ConnectionProfile>>([]);
        public Task<ConnectionProfile?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<ConnectionProfile?>(null);

        public Task<ConnectionProfile> SaveAsync(ConnectionProfileRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new ConnectionProfile(
                request.Id ?? Guid.NewGuid(),
                request.Name,
                request.Host,
                request.Port,
                request.Database,
                request.UserName,
                request.Charset,
                request.Role,
                request.RememberPassword,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow));
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeCredentialStore : ICredentialStore
    {
        public Task SaveAsync(Guid profileId, CredentialSecret secret, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<CredentialSecret?> TryLoadAsync(Guid profileId, CancellationToken cancellationToken) => Task.FromResult<CredentialSecret?>(null);
        public Task DeleteAsync(Guid profileId, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeFirebirdConnectionService(bool shouldFail) : IFirebirdConnectionService
    {
        public Task<ConnectionContext> ConnectAsync(ConnectionRequest request, CancellationToken cancellationToken)
        {
            if (shouldFail)
            {
                throw new InvalidOperationException("Falha simulada");
            }

            return Task.FromResult(new ConnectionContext(
                request.Profile.Id,
                request.Profile.Name,
                request.Profile.Host,
                request.Profile.Port,
                request.Profile.Database,
                request.Profile.UserName,
                FirebirdServerVersion.Parse("5.0.0"),
                new FirebirdCapabilities(true, true, true, true, true, "Capabilities resolvidas para teste."),
                new EffectiveToolset([]),
                DateTimeOffset.UtcNow));
        }
    }

    private sealed class FakeMonitoringSessionService : IMonitoringSessionService
    {
        private readonly MonitoringSnapshot snapshot = new(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            [],
            [new TransactionSnapshot(42, 7, "1", DateTimeOffset.UtcNow, 1, 2, 3, 4)],
            []);

        public MonitoringSessionStatus Status { get; private set; } = new(PollingState.Stopped, "Stopped", DateTimeOffset.UtcNow);

        public Task<MonitoringSession> StartAsync(
            ConnectionContext connection,
            ConnectionProfile profile,
            CredentialSecret? password,
            PollingOptions options,
            CancellationToken cancellationToken)
        {
            Status = new MonitoringSessionStatus(PollingState.Connected, "Connected", DateTimeOffset.UtcNow);
            return Task.FromResult(new MonitoringSession(Guid.NewGuid(), connection, options, DateTimeOffset.UtcNow));
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Status = new MonitoringSessionStatus(PollingState.Stopped, "Stopped", DateTimeOffset.UtcNow);
            return Task.CompletedTask;
        }

        public async IAsyncEnumerable<MonitoringSnapshot> ReadAllAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield return snapshot;
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }
    }

    private sealed class FakeProfilerSessionService : IProfilerSessionService
    {
        public ProfilerState State { get; private set; } = ProfilerState.Disconnected;

        public Task<ProfilerSession> StartAsync(ProfilerOptions options, CredentialSecret? password, CancellationToken cancellationToken)
        {
            State = ProfilerState.Running;
            return Task.FromResult(new ProfilerSession(Guid.NewGuid(), options.SessionName, DateTimeOffset.UtcNow, State));
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            State = ProfilerState.Ready;
            return Task.CompletedTask;
        }

        public async IAsyncEnumerable<ProfilerEvent> ReadAllAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield return new ProfilerEvent(1, DateTimeOffset.UtcNow, TraceEventType.StatementFinished, TimeSpan.FromMilliseconds(2), "SYSDBA", 7, 8, "select 1 from rdb$database", new ProfilerMetrics(), null, "raw");
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }
    }

    private sealed class FakeHistoryWriter : IHistoryWriter
    {
        public Task WriteProfilerEventsAsync(Guid? connectionProfileId, IReadOnlyList<ProfilerEvent> events, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task WriteMonitoringSnapshotsAsync(Guid? connectionProfileId, IReadOnlyList<MonitoringSnapshot> snapshots, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeHistoryQueryService : IHistoryQueryService
    {
        public Task<HistoryPage<TraceEventHistoryItem>> QueryTraceEventsAsync(HistoryQuery query, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HistoryPage<TraceEventHistoryItem>([], query.Page, query.PageSize, 0));
        }

        public Task<HistoryPage<MonitoringSnapshotHistoryItem>> QueryMonitoringSnapshotsAsync(HistoryQuery query, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HistoryPage<MonitoringSnapshotHistoryItem>([], query.Page, query.PageSize, 0));
        }
    }

    private sealed class FakeHistoryExportService : IHistoryExportService
    {
        public Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new ExportResult("fake.csv", 0));
        }
    }
}

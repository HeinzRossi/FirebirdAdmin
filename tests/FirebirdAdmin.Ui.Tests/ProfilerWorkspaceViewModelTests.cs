using FirebirdAdmin.Application.Connections;
using FirebirdAdmin.Application.History;
using FirebirdAdmin.Application.Monitoring;
using FirebirdAdmin.Application.Profiler;
using FirebirdAdmin.Presentation.Wpf.Profiler;
using FirebirdAdmin.Presentation.Wpf.Resources;
using FluentAssertions;

namespace FirebirdAdmin.Ui.Tests;

public sealed class ProfilerWorkspaceViewModelTests
{
    [Fact]
    public void Constructor_ShouldStartDisconnected()
    {
        var viewModel = new ProfilerWorkspaceViewModel(new FakeProfilerSessionService(), new FakeHistoryWriter());

        viewModel.State.Should().Be(ProfilerState.Disconnected);
        viewModel.Message.Should().Contain("Conecte");
        viewModel.CanStart.Should().BeFalse();
        viewModel.CanStop.Should().BeFalse();
        viewModel.CanPauseOrResume.Should().BeFalse();
        viewModel.PauseResumeLabel.Should().Be(AppStrings.PauseView);
    }

    [Fact]
    public void SetReady_ShouldEnableStartOnly()
    {
        var viewModel = new ProfilerWorkspaceViewModel(new FakeProfilerSessionService(), new FakeHistoryWriter());

        viewModel.SetReady();

        viewModel.State.Should().Be(ProfilerState.Ready);
        viewModel.CanStart.Should().BeTrue();
        viewModel.CanStop.Should().BeFalse();
        viewModel.CanPauseOrResume.Should().BeFalse();
    }

    [Fact]
    public async Task StartAsync_ShouldReadEventsAndFollowLatest()
    {
        var viewModel = new ProfilerWorkspaceViewModel(new FakeProfilerSessionService(), new FakeHistoryWriter());

        using var password = CredentialSecret.FromPlainText("masterkey");
        await viewModel.StartAsync(CreateConnection(), password, CancellationToken.None);
        await WaitUntilAsync(() => viewModel.EventCount == 1);

        viewModel.State.Should().Be(ProfilerState.Running);
        viewModel.CanStart.Should().BeFalse();
        viewModel.CanStop.Should().BeTrue();
        viewModel.CanPauseOrResume.Should().BeTrue();
        viewModel.PauseResumeLabel.Should().Be(AppStrings.PauseView);
        viewModel.SelectedEvent.Should().NotBeNull();
        viewModel.SelectedSql.Should().Contain("select");
    }

    [Fact]
    public async Task PauseView_ShouldFreezeVisibleEventsButKeepBuffer()
    {
        var service = new FakeProfilerSessionService(twoEvents: true);
        var viewModel = new ProfilerWorkspaceViewModel(service, new FakeHistoryWriter());

        using var password = CredentialSecret.FromPlainText("masterkey");
        await viewModel.StartAsync(CreateConnection(), password, CancellationToken.None);
        await WaitUntilAsync(() => viewModel.EventCount == 1);
        viewModel.PauseView();
        service.ReleaseSecondEvent();
        await WaitUntilAsync(() => viewModel.BufferedCount == 2);

        viewModel.State.Should().Be(ProfilerState.PausedView);
        viewModel.EventCount.Should().Be(1);
        viewModel.CanStart.Should().BeFalse();
        viewModel.CanStop.Should().BeTrue();
        viewModel.CanPauseOrResume.Should().BeTrue();
        viewModel.PauseResumeLabel.Should().Be(AppStrings.ResumeView);

        viewModel.TogglePauseResume();

        viewModel.State.Should().Be(ProfilerState.Running);
        viewModel.EventCount.Should().Be(2);
        viewModel.SelectedEvent!.Sequence.Should().Be(2);
        viewModel.PauseResumeLabel.Should().Be(AppStrings.PauseView);
    }

    [Fact]
    public async Task StopAsync_ShouldEnableStartAndDisableStop()
    {
        var service = new FakeProfilerSessionService();
        var viewModel = new ProfilerWorkspaceViewModel(service, new FakeHistoryWriter());

        using var password = CredentialSecret.FromPlainText("masterkey");
        await viewModel.StartAsync(CreateConnection(), password, CancellationToken.None);
        await viewModel.StopAsync(CancellationToken.None);

        viewModel.State.Should().Be(ProfilerState.Ready);
        viewModel.CanStart.Should().BeTrue();
        viewModel.CanStop.Should().BeFalse();
        viewModel.CanPauseOrResume.Should().BeFalse();
    }

    [Fact]
    public async Task StartAsync_ShouldFailWithClearMessage_WhenPasswordIsMissing()
    {
        var service = new FakeProfilerSessionService();
        var viewModel = new ProfilerWorkspaceViewModel(service, new FakeHistoryWriter());

        await viewModel.StartAsync(CreateConnection(), null, CancellationToken.None);

        viewModel.State.Should().Be(ProfilerState.Failed);
        viewModel.Message.Should().Contain("senha da sessão");
        service.State.Should().Be(ProfilerState.Disconnected);
    }

    private static ConnectionContext CreateConnection()
    {
        return new ConnectionContext(
            Guid.NewGuid(),
            "Local",
            "localhost",
            3050,
            "employee.fdb",
            "SYSDBA",
            FirebirdServerVersion.Parse("5.0.0"),
            new FirebirdCapabilities(true, true, true, true, true, "ok"),
            EffectiveToolset.Empty,
            DateTimeOffset.UtcNow);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        while (!condition())
        {
            await Task.Delay(20, timeout.Token);
        }
    }

    private sealed class FakeProfilerSessionService(bool twoEvents = false) : IProfilerSessionService
    {
        private readonly TaskCompletionSource secondEventReleased = new(TaskCreationOptions.RunContinuationsAsynchronously);

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

        public void ReleaseSecondEvent()
        {
            secondEventReleased.TrySetResult();
        }

        public async IAsyncEnumerable<ProfilerEvent> ReadAllAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield return CreateEvent(1);

            if (twoEvents)
            {
                await secondEventReleased.Task.WaitAsync(cancellationToken);
                yield return CreateEvent(2);
            }

            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }

        private static ProfilerEvent CreateEvent(long sequence)
        {
            return new ProfilerEvent(
                sequence,
                DateTimeOffset.UtcNow,
                TraceEventType.StatementFinished,
                TimeSpan.FromMilliseconds(sequence),
                "SYSDBA",
                1,
                2,
                $"select {sequence} from rdb$database",
                new ProfilerMetrics(),
                null,
                "raw");
        }
    }

    private sealed class FakeHistoryWriter : IHistoryWriter
    {
        public List<ProfilerEvent> ProfilerEvents { get; } = [];
        public Task WriteProfilerEventsAsync(Guid? connectionProfileId, IReadOnlyList<ProfilerEvent> events, CancellationToken cancellationToken)
        {
            ProfilerEvents.AddRange(events);
            return Task.CompletedTask;
        }

        public Task WriteMonitoringSnapshotsAsync(Guid? connectionProfileId, IReadOnlyList<MonitoringSnapshot> snapshots, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}

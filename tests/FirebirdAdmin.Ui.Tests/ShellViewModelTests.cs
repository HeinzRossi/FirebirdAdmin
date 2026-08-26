using FirebirdAdmin.Application.Connections;
using FirebirdAdmin.Application.Dashboard;
using FirebirdAdmin.Application.Diagnostics;
using FirebirdAdmin.Application.History;
using FirebirdAdmin.Application.Maintenance;
using FirebirdAdmin.Application.Metadata;
using FirebirdAdmin.Application.Monitoring;
using FirebirdAdmin.Application.Profiler;
using FirebirdAdmin.Application.Security;
using FirebirdAdmin.Presentation.Wpf.Dashboard;
using FirebirdAdmin.Presentation.Wpf.Diagnostics;
using FirebirdAdmin.Presentation.Wpf.History;
using FirebirdAdmin.Presentation.Wpf.Maintenance;
using FirebirdAdmin.Presentation.Wpf.Metadata;
using FirebirdAdmin.Presentation.Wpf.Monitoring;
using FirebirdAdmin.Presentation.Wpf.Profiler;
using FirebirdAdmin.Presentation.Wpf.Security;
using FirebirdAdmin.Presentation.Wpf.Resources;
using FirebirdAdmin.Presentation.Wpf.Shell;
using FirebirdAdmin.Presentation.Wpf.Theme;
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
        viewModel.SelectedWorkspace.Should().Be(ShellWorkspace.Dashboard);
        viewModel.WorkspaceTitle.Should().Be(AppStrings.Dashboard);
        viewModel.NavigationItems.Should().HaveCount(9);
        viewModel.NavigationItems.Should().OnlyContain(item => !string.IsNullOrWhiteSpace(item.IconGlyph));
        viewModel.NavigationItems.Should().OnlyContain(item => !item.AccessText.Contains(item.Shortcut, StringComparison.Ordinal));
        viewModel.ExitLabel.Should().Be(AppStrings.Exit);
        viewModel.AboutLabel.Should().Be(AppStrings.About);
        viewModel.IsAboutOpen.Should().BeFalse();
        viewModel.CurrentTheme.Should().Be(AppTheme.Dark);
        viewModel.ThemeToggleLabel.Should().Contain(AppStrings.ThemeDark);
        viewModel.TitleBarVersionText.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void AboutCommands_ShouldToggleAboutState()
    {
        var viewModel = CreateViewModel();

        viewModel.ShowAbout();
        viewModel.IsAboutOpen.Should().BeTrue();

        viewModel.CloseAbout();
        viewModel.IsAboutOpen.Should().BeFalse();
    }

    [Fact]
    public void SelectWorkspace_ShouldUpdateSelectedItemAndTitle()
    {
        var viewModel = CreateViewModel();

        viewModel.SelectWorkspace(ShellWorkspace.SqlProfiler);

        viewModel.SelectedWorkspace.Should().Be(ShellWorkspace.SqlProfiler);
        viewModel.SelectedNavigationItem.Should().NotBeNull();
        viewModel.SelectedNavigationItem!.Title.Should().Be(AppStrings.SqlProfiler);
        viewModel.WorkspaceTitle.Should().Be(AppStrings.SqlProfiler);
    }

    [Theory]
    [InlineData("1", ShellWorkspace.Dashboard)]
    [InlineData("2", ShellWorkspace.Monitoring)]
    [InlineData("3", ShellWorkspace.SqlProfiler)]
    [InlineData("4", ShellWorkspace.Diagnostics)]
    [InlineData("5", ShellWorkspace.Metadata)]
    [InlineData("6", ShellWorkspace.Security)]
    [InlineData("7", ShellWorkspace.Maintenance)]
    [InlineData("8", ShellWorkspace.History)]
    [InlineData("9", ShellWorkspace.Settings)]
    public void SelectWorkspaceByShortcut_ShouldMapCtrlNumberOrder(string shortcut, ShellWorkspace expected)
    {
        var viewModel = CreateViewModel();

        viewModel.SelectWorkspaceByShortcut(shortcut);

        viewModel.SelectedWorkspace.Should().Be(expected);
    }

    [Fact]
    public async Task RefreshSelectedWorkspaceAsync_ShouldRefreshCurrentWorkspaceOnly()
    {
        var metadataService = new FakeMetadataCatalogService();
        var securityService = new FakeSecurityCatalogService();
        var viewModel = CreateViewModel(metadataService: metadataService, securityService: securityService);

        await viewModel.ConnectAsync("masterkey");
        viewModel.SelectWorkspace(ShellWorkspace.Metadata);
        await viewModel.RefreshSelectedWorkspaceAsync();

        metadataService.LoadCount.Should().BeGreaterThan(0);

        viewModel.SelectWorkspace(ShellWorkspace.Security);
        await viewModel.RefreshSelectedWorkspaceAsync();

        securityService.LoadCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SwitchingWorkspace_ShouldPreserveProfilerState()
    {
        var viewModel = CreateViewModel();
        await viewModel.ConnectAsync("masterkey");
        await viewModel.StartProfilerAsync(string.Empty);
        await WaitUntilAsync(() => viewModel.ProfilerWorkspace.EventCount == 1);

        viewModel.SelectWorkspace(ShellWorkspace.History);
        viewModel.SelectWorkspace(ShellWorkspace.SqlProfiler);

        viewModel.ProfilerWorkspace.EventCount.Should().Be(1);
        viewModel.ProfilerWorkspace.SelectedEvent.Should().NotBeNull();
    }

    [Fact]
    public async Task StartProfilerAsync_ShouldReuseActiveConnectionPasswordWithoutPromptingAgain()
    {
        var profilerService = new FakeProfilerSessionService();
        var viewModel = CreateViewModel(profilerSessionService: profilerService);

        await viewModel.ConnectAsync("masterkey");
        await viewModel.StartProfilerAsync(string.Empty);

        profilerService.StartCount.Should().Be(1);
        profilerService.LastPasswordLength.Should().Be("masterkey".Length);
        viewModel.ProfilerWorkspace.State.Should().Be(ProfilerState.Running);
    }

    [Fact]
    public async Task StartProfilerAsync_ShouldUseSavedPassword_WhenSessionPasswordIsUnavailable()
    {
        var profilerService = new FakeProfilerSessionService();
        var profileId = Guid.NewGuid();
        var profileService = new FakeConnectionProfileService(
            new ConnectionProfile(
                profileId,
                "Local",
                "localhost",
                3050,
                "employee.fdb",
                "SYSDBA",
                "UTF8",
                null,
                true,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow));
        var credentialStore = new FakeCredentialStore("saved-secret");
        var viewModel = CreateViewModel(
            connectionProfileService: profileService,
            credentialStore: credentialStore,
            profilerSessionService: profilerService);

        await viewModel.LoadInitialProfileAsync();
        await viewModel.ConnectAsync("masterkey");
        viewModel.ClearSessionCredentialForShutdown();
        await viewModel.StartProfilerAsync(string.Empty);

        profilerService.StartCount.Should().Be(1);
        profilerService.LastPasswordLength.Should().Be("masterkey".Length);
    }

    [Fact]
    public async Task LoadInitialProfileAsync_ShouldPopulateSavedProfileWithoutRevealingPassword()
    {
        var profile = new ConnectionProfile(
            Guid.NewGuid(),
            "Produção",
            "db-server",
            3051,
            "prod.fdb",
            "SYSDBA",
            "WIN1252",
            "ADMIN",
            true,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);
        var viewModel = CreateViewModel(connectionProfileService: new FakeConnectionProfileService(profile));

        await viewModel.LoadInitialProfileAsync();

        viewModel.ProfileName.Should().Be("Produção");
        viewModel.Host.Should().Be("db-server");
        viewModel.Port.Should().Be(3051);
        viewModel.Database.Should().Be("prod.fdb");
        viewModel.UserName.Should().Be("SYSDBA");
        viewModel.Charset.Should().Be("WIN1252");
        viewModel.Role.Should().Be("ADMIN");
        viewModel.RememberPassword.Should().BeTrue();
        viewModel.HasSavedPasswordForProfile.Should().BeTrue();
        viewModel.PasswordStatusText.Should().Be(AppStrings.PasswordSavedForProfile);
    }

    [Fact]
    public async Task ConnectAsync_ShouldUseSavedPasswordWithoutRevealingItInThePasswordBox()
    {
        var profile = new ConnectionProfile(
            Guid.NewGuid(),
            "Local",
            "localhost",
            3050,
            "employee.fdb",
            "SYSDBA",
            "UTF8",
            null,
            true,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);
        var profileService = new FakeConnectionProfileService(profile);
        var connectionService = new FakeFirebirdConnectionService(false);
        var credentialStore = new FakeCredentialStore("saved-secret");
        var viewModel = CreateViewModel(
            connectionProfileService: profileService,
            credentialStore: credentialStore,
            firebirdConnectionService: connectionService);

        await viewModel.LoadInitialProfileAsync();
        await viewModel.ConnectAsync(string.Empty);

        connectionService.ConnectionCount.Should().Be(1);
        connectionService.LastPasswordLength.Should().Be("saved-secret".Length);
        profileService.LastRequest.Should().NotBeNull();
        credentialStore.SaveCount.Should().Be(0);
        credentialStore.DeleteCount.Should().Be(0);
        viewModel.HasSavedPasswordForProfile.Should().BeTrue();
        viewModel.OperationMessage.Should().Be(AppStrings.ConnectedStatus);
    }

    [Fact]
    public async Task ConnectAsync_ShouldSaveTypedPasswordAfterSuccessfulConnection_WhenRememberPasswordIsChecked()
    {
        var credentialStore = new FakeCredentialStore();
        var viewModel = CreateViewModel(credentialStore: credentialStore);
        viewModel.RememberPassword = true;

        await viewModel.ConnectAsync("masterkey");

        credentialStore.SaveCount.Should().Be(1);
        credentialStore.SavedPasswordLength.Should().Be("masterkey".Length);
        viewModel.HasSavedPasswordForProfile.Should().BeTrue();
        viewModel.RememberPassword.Should().BeTrue();
    }

    [Fact]
    public async Task ConnectAsync_ShouldNotSaveOrDeletePassword_WhenRememberPasswordIsUnchecked()
    {
        var credentialStore = new FakeCredentialStore();
        var viewModel = CreateViewModel(credentialStore: credentialStore);
        viewModel.RememberPassword = false;

        await viewModel.ConnectAsync("masterkey");

        credentialStore.SaveCount.Should().Be(0);
        credentialStore.DeleteCount.Should().Be(0);
        viewModel.HasSavedPasswordForProfile.Should().BeFalse();
    }

    [Fact]
    public async Task ConnectAsync_ShouldNotTryConnection_WhenNoPasswordIsAvailable()
    {
        var connectionService = new FakeFirebirdConnectionService(false);
        var viewModel = CreateViewModel(firebirdConnectionService: connectionService);

        await viewModel.ConnectAsync(string.Empty);

        connectionService.ConnectionCount.Should().Be(0);
        viewModel.ConnectionState.Should().Be(ShellConnectionState.ConnectionFailed);
        viewModel.OperationMessage.Should().Be(AppStrings.PasswordRequired);
    }

    [Fact]
    public async Task SaveProfileAsync_ShouldDeleteSavedPasswordOnlyWhenExplicitlySavingUnchecked()
    {
        var profile = new ConnectionProfile(
            Guid.NewGuid(),
            "Local",
            "localhost",
            3050,
            "employee.fdb",
            "SYSDBA",
            "UTF8",
            null,
            true,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);
        var profileService = new FakeConnectionProfileService(profile);
        var credentialStore = new FakeCredentialStore("saved-secret", profileService.MarkPasswordDeleted);
        var viewModel = CreateViewModel(
            connectionProfileService: profileService,
            credentialStore: credentialStore);

        await viewModel.LoadInitialProfileAsync();
        viewModel.RememberPassword = false;
        await viewModel.SaveProfileAsync(string.Empty);

        credentialStore.DeleteCount.Should().BeGreaterThanOrEqualTo(1);
        viewModel.HasSavedPasswordForProfile.Should().BeFalse();
        viewModel.RememberPassword.Should().BeFalse();
    }

    [Fact]
    public async Task RememberPasswordUnchecked_ShouldNotDeleteSavedPasswordUntilProfileIsSaved()
    {
        var profile = new ConnectionProfile(
            Guid.NewGuid(),
            "Local",
            "localhost",
            3050,
            "employee.fdb",
            "SYSDBA",
            "UTF8",
            null,
            true,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);
        var profileService = new FakeConnectionProfileService(profile);
        var credentialStore = new FakeCredentialStore("saved-secret", profileService.MarkPasswordDeleted);
        var viewModel = CreateViewModel(
            connectionProfileService: profileService,
            credentialStore: credentialStore);

        await viewModel.LoadInitialProfileAsync();

        viewModel.RememberPassword = false;
        await viewModel.ConnectAsync("masterkey");

        credentialStore.DeleteCount.Should().Be(0);
        credentialStore.SaveCount.Should().Be(0);
        viewModel.RememberPassword.Should().BeFalse();
        viewModel.HasSavedPasswordForProfile.Should().BeTrue();
        viewModel.PasswordStatusText.Should().Be(AppStrings.PasswordSavedForProfile);
    }

    [Fact]
    public async Task TestConnectionAsync_ShouldNotPersistANewProfileOrPassword()
    {
        var profileService = new FakeConnectionProfileService();
        var credentialStore = new FakeCredentialStore();
        var connectionService = new FakeFirebirdConnectionService(false);
        var viewModel = CreateViewModel(
            connectionProfileService: profileService,
            credentialStore: credentialStore,
            firebirdConnectionService: connectionService);
        viewModel.RememberPassword = true;

        await viewModel.TestConnectionAsync("masterkey");

        connectionService.ConnectionCount.Should().Be(1);
        profileService.LastRequest.Should().BeNull();
        credentialStore.SaveCount.Should().Be(0);
        credentialStore.DeleteCount.Should().Be(0);
        viewModel.ActiveConnection.Should().BeNull();
        viewModel.OperationMessage.Should().Be(AppStrings.TestConnectionSucceeded);
    }

    [Fact]
    public async Task ConnectAsync_ShouldSetConnectedStateAndContext()
    {
        var metadataService = new FakeMetadataCatalogService();
        var securityService = new FakeSecurityCatalogService();
        var viewModel = CreateViewModel(metadataService: metadataService, securityService: securityService);
        viewModel.Database = "employee.fdb";

        await viewModel.ConnectAsync("masterkey");

        viewModel.ConnectionState.Should().Be(ShellConnectionState.Connected);
        viewModel.HasActiveConnection.Should().BeTrue();
        viewModel.ConnectionContext.Should().Contain("Firebird");
        await WaitUntilAsync(() => viewModel.TransactionsWorkspace.State == TransactionsWorkspaceState.Ready);
        viewModel.Dashboard.Health.Should().Be(DatabaseHealthStatus.Healthy);
        viewModel.Dashboard.Metrics.Should().Contain(metric => metric.Key == "transactions" && metric.Value == "1");
        await WaitUntilAsync(() => metadataService.LoadCount > 0 && securityService.LoadCount > 0);
        metadataService.LastPasswordLength.Should().BeGreaterThan(0);
        securityService.LastPasswordLength.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ToggleTheme_ShouldSwitchThemeAndUpdateLabel()
    {
        var themeService = new FakeThemeService();
        var viewModel = CreateViewModel(themeService: themeService);

        viewModel.ToggleTheme();

        viewModel.CurrentTheme.Should().Be(AppTheme.Light);
        themeService.CurrentTheme.Should().Be(AppTheme.Light);
        viewModel.ThemeToggleLabel.Should().Contain(AppStrings.ThemeLight);
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

    [Fact]
    public async Task ShutdownAsync_ShouldStopProfilerAndMonitoringAndBeIdempotent()
    {
        var profilerService = new FakeProfilerSessionService();
        var monitoringService = new FakeMonitoringSessionService();
        var viewModel = CreateViewModel(
            monitoringSessionService: monitoringService,
            profilerSessionService: profilerService);

        await viewModel.ConnectAsync("masterkey");
        await viewModel.StartProfilerAsync(string.Empty);

        await viewModel.ShutdownAsync();
        await viewModel.ShutdownAsync();

        profilerService.StopCount.Should().Be(1);
        monitoringService.StopCount.Should().BeGreaterThanOrEqualTo(1);
        viewModel.HasActiveConnection.Should().BeFalse();
        viewModel.ConnectionState.Should().Be(ShellConnectionState.Disconnected);
    }

    private static ShellViewModel CreateViewModel(
        bool connectionShouldFail = false,
        FakeMetadataCatalogService? metadataService = null,
        FakeSecurityCatalogService? securityService = null,
        IConnectionProfileService? connectionProfileService = null,
        ICredentialStore? credentialStore = null,
        IFirebirdConnectionService? firebirdConnectionService = null,
        IMonitoringSessionService? monitoringSessionService = null,
        IProfilerSessionService? profilerSessionService = null,
        IThemeService? themeService = null)
    {
        return new ShellViewModel(
            connectionProfileService ?? new FakeConnectionProfileService(),
            credentialStore ?? new FakeCredentialStore(),
            firebirdConnectionService ?? new FakeFirebirdConnectionService(connectionShouldFail),
            monitoringSessionService ?? new FakeMonitoringSessionService(),
            new FakeHistoryWriter(),
            new DiagnosticEngine([new FakeDiagnosticRule()]),
            new TransactionsWorkspaceViewModel(),
            new DashboardViewModel(new DashboardProjectionService()),
            new ProfilerWorkspaceViewModel(profilerSessionService ?? new FakeProfilerSessionService(), new FakeHistoryWriter()),
            new HistoryWorkspaceViewModel(new FakeHistoryQueryService(), new FakeHistoryExportService()),
            new AlertsCenterViewModel(new FakeAlertStore()),
            new MetadataExplorerViewModel(metadataService ?? new FakeMetadataCatalogService()),
            new MaintenanceWorkspaceViewModel(new FakeMaintenanceService(), new FakeMaintenanceHistoryStore()),
            new SecurityWorkspaceViewModel(securityService ?? new FakeSecurityCatalogService()),
            themeService ?? new FakeThemeService());
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
        private readonly List<ConnectionProfile> profiles;

        public FakeConnectionProfileService(params ConnectionProfile[] profiles)
        {
            this.profiles = profiles.ToList();
        }

        public ConnectionProfileRequest? LastRequest { get; private set; }

        public Task<IReadOnlyList<ConnectionProfile>> ListAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<ConnectionProfile>>(profiles.ToArray());
        }

        public Task<ConnectionProfile?> GetAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(profiles.SingleOrDefault(profile => profile.Id == id));
        }

        public Task<ConnectionProfile> SaveAsync(ConnectionProfileRequest request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            var existing = profiles.SingleOrDefault(profile => profile.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase));
            var hasSavedPassword = request.Password is not null || existing?.HasSavedPassword == true;
            var profile = new ConnectionProfile(
                existing?.Id ?? request.Id ?? Guid.NewGuid(),
                request.Name,
                request.Host,
                request.Port,
                request.Database,
                request.UserName,
                request.Charset,
                request.Role,
                hasSavedPassword,
                existing?.CreatedAt ?? DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow);

            if (existing is not null)
            {
                profiles.Remove(existing);
            }

            profiles.Add(profile);
            return Task.FromResult(profile);
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;

        public void MarkPasswordDeleted(Guid id)
        {
            var existing = profiles.SingleOrDefault(profile => profile.Id == id);
            if (existing is null)
            {
                return;
            }

            profiles.Remove(existing);
            profiles.Add(existing with { HasSavedPassword = false, UpdatedAt = DateTimeOffset.UtcNow });
        }
    }

    private sealed class FakeThemeService : IThemeService
    {
        public AppTheme CurrentTheme { get; private set; } = AppTheme.Dark;

        public void Apply(AppTheme theme)
        {
            CurrentTheme = theme;
        }

        public AppTheme Toggle()
        {
            CurrentTheme = CurrentTheme == AppTheme.Light ? AppTheme.Dark : AppTheme.Light;
            return CurrentTheme;
        }
    }

    private sealed class FakeCredentialStore(string? savedPassword = null, Action<Guid>? onDelete = null) : ICredentialStore
    {
        public int SaveCount { get; private set; }
        public int DeleteCount { get; private set; }
        public int SavedPasswordLength { get; private set; }

        public Task SaveAsync(Guid profileId, CredentialSecret secret, CancellationToken cancellationToken)
        {
            SaveCount++;
            var bytes = secret.CopyBytes();
            SavedPasswordLength = bytes.Length;
            savedPassword = System.Text.Encoding.UTF8.GetString(bytes);
            Array.Clear(bytes);
            return Task.CompletedTask;
        }

        public Task<CredentialSecret?> TryLoadAsync(Guid profileId, CancellationToken cancellationToken)
        {
            return Task.FromResult(string.IsNullOrEmpty(savedPassword)
                ? null
                : CredentialSecret.FromPlainText(savedPassword));
        }

        public Task DeleteAsync(Guid profileId, CancellationToken cancellationToken)
        {
            DeleteCount++;
            savedPassword = null;
            onDelete?.Invoke(profileId);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeFirebirdConnectionService(bool shouldFail) : IFirebirdConnectionService
    {
        public int ConnectionCount { get; private set; }
        public int LastPasswordLength { get; private set; }

        public Task<ConnectionContext> ConnectAsync(ConnectionRequest request, CancellationToken cancellationToken)
        {
            if (shouldFail)
            {
                throw new InvalidOperationException("Falha simulada");
            }

            ConnectionCount++;
            LastPasswordLength = request.Password?.CopyBytes().Length ?? 0;
            LastPasswordLength.Should().BeGreaterThan(0);
            request.Password?.Dispose();

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
        public int StopCount { get; private set; }

        public Task<MonitoringSession> StartAsync(
            ConnectionContext connection,
            ConnectionProfile profile,
            CredentialSecret? password,
            PollingOptions options,
            CancellationToken cancellationToken)
        {
            password?.CopyBytes().Should().NotBeEmpty();
            Status = new MonitoringSessionStatus(PollingState.Connected, "Connected", DateTimeOffset.UtcNow);
            return Task.FromResult(new MonitoringSession(Guid.NewGuid(), connection, options, DateTimeOffset.UtcNow));
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            StopCount++;
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
        public int StartCount { get; private set; }
        public int StopCount { get; private set; }
        public int LastPasswordLength { get; private set; }
        public ProfilerState State { get; private set; } = ProfilerState.Disconnected;

        public Task<ProfilerSession> StartAsync(ProfilerOptions options, CredentialSecret? password, CancellationToken cancellationToken)
        {
            StartCount++;
            LastPasswordLength = password?.CopyBytes().Length ?? 0;
            State = ProfilerState.Running;
            return Task.FromResult(new ProfilerSession(Guid.NewGuid(), options.SessionName, DateTimeOffset.UtcNow, State));
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            StopCount++;
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

    private sealed class FakeDiagnosticRule : IDiagnosticRule
    {
        public string RuleId => "TEST_RULE";

        public IReadOnlyList<DiagnosticResult> Evaluate(DiagnosticContext context, DiagnosticRuleOptions options)
        {
            if (context.MonitoringSnapshot is null)
            {
                return [];
            }

            return
            [
                new DiagnosticResult(
                    RuleId,
                    DiagnosticSeverity.Low,
                    "Teste",
                    new DiagnosticTarget("Session", context.MonitoringSnapshot.SessionId.ToString("N")),
                    DateTimeOffset.UtcNow,
                    context.ConnectionProfileId,
                    context.MonitoringSnapshot.SessionId,
                    [new DiagnosticEvidence("Count", 1)])
            ];
        }
    }

    private sealed class FakeAlertStore : IAlertStore
    {
        private readonly List<Alert> alerts = [];
        private readonly AlertCorrelator correlator = new();

        public Task<Alert> UpsertAsync(DiagnosticResult result, CancellationToken cancellationToken)
        {
            var key = AlertCorrelator.BuildCorrelationKey(result);
            var existing = alerts.SingleOrDefault(alert => alert.CorrelationKey == key);
            var alert = correlator.Correlate(result, existing);
            if (existing is not null)
            {
                alerts.Remove(existing);
            }

            alerts.Add(alert);
            return Task.FromResult(alert);
        }

        public Task<IReadOnlyList<Alert>> ListAsync(AlertStatus? status, DiagnosticSeverity? severity, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<Alert>>(alerts.Where(alert =>
                (status is null || alert.Status == status) &&
                (severity is null || alert.Severity == severity)).ToArray());
        }

        public Task<Alert?> GetByCorrelationKeyAsync(string correlationKey, CancellationToken cancellationToken)
        {
            return Task.FromResult(alerts.SingleOrDefault(alert => alert.CorrelationKey == correlationKey));
        }

        public Task SetStatusAsync(Guid id, AlertStatus status, string? note, CancellationToken cancellationToken)
        {
            var alert = alerts.SingleOrDefault(item => item.Id == id);
            if (alert is not null)
            {
                alerts.Remove(alert);
                alerts.Add(alert with { Status = status, AcknowledgementNote = note });
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FakeMetadataCatalogService : IMetadataCatalogService
    {
        private MetadataCatalog? catalog;
        public int LoadCount { get; private set; }
        public int LastPasswordLength { get; private set; }

        public Task<MetadataCatalog> LoadCatalogAsync(ConnectionContext connection, CredentialSecret? password, CancellationToken cancellationToken)
        {
            LoadCount++;
            LastPasswordLength = password?.CopyBytes().Length ?? 0;
            catalog = new MetadataCatalog(
                [new MetadataObjectSummary(new MetadataObjectReference(MetadataObjectKind.Table, "CUSTOMERS"), "CUSTOMERS")],
                DateTimeOffset.UtcNow,
                MetadataCacheState.Fresh);
            return Task.FromResult(catalog);
        }

        public Task<MetadataObjectDetails> LoadDetailsAsync(
            ConnectionContext connection,
            CredentialSecret? password,
            MetadataObjectReference reference,
            CancellationToken cancellationToken)
        {
            var summary = new MetadataObjectSummary(reference, reference.Name);
            return Task.FromResult(new MetadataObjectDetails(summary, [], [], [], [], [], [], null, null));
        }

        public MetadataCatalog? GetCachedCatalog() => catalog;

        public void MarkCacheStale()
        {
            if (catalog is not null)
            {
                catalog = catalog with { State = MetadataCacheState.Stale };
            }
        }
    }

    private sealed class FakeMaintenanceService : IMaintenanceService
    {
        public MaintenanceOperation? ActiveOperation => null;
        public event EventHandler<MaintenanceProgress>? ProgressChanged;
        public event EventHandler<MaintenanceLogLine>? LogReceived;

        public Task<MaintenancePreflightResult> ValidateAsync(MaintenanceRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new MaintenancePreflightResult(true, [], [], ["ok"]));
        }

        public Task<MaintenanceResult> ExecuteAsync(MaintenanceRequest request, CredentialSecret? password, CancellationToken cancellationToken)
        {
            var operation = new MaintenanceOperation(Guid.NewGuid(), request.Connection.ProfileId, request.Type, MaintenanceOperationStatus.Succeeded, request.Source, request.Target, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 0, "ok");
            ProgressChanged?.Invoke(this, new MaintenanceProgress(operation.Id, "Resultado", 1, "ok", DateTimeOffset.UtcNow));
            LogReceived?.Invoke(this, new MaintenanceLogLine(operation.Id, DateTimeOffset.UtcNow, "stdout", "ok"));
            return Task.FromResult(new MaintenanceResult(operation, []));
        }
    }

    private sealed class FakeMaintenanceHistoryStore : IMaintenanceHistoryStore
    {
        public Task SaveOperationAsync(MaintenanceOperation operation, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SaveLogAsync(MaintenanceLogLine logLine, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IReadOnlyList<MaintenanceOperation>> ListRecentAsync(int take, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<MaintenanceOperation>>([]);
    }

    private sealed class FakeSecurityCatalogService : ISecurityCatalogService
    {
        private SecurityCatalog? catalog;
        public int LoadCount { get; private set; }
        public int LastPasswordLength { get; private set; }

        public Task<SecurityCatalog> LoadCatalogAsync(ConnectionContext connection, CredentialSecret? password, CancellationToken cancellationToken)
        {
            LoadCount++;
            LastPasswordLength = password?.CopyBytes().Length ?? 0;
            catalog = new SecurityCatalog(
                [new SecurityUser("SYSDBA", "SEC$USERS", true)],
                [new SecurityRole("RDB$ADMIN", "SYSDBA")],
                [new SecurityGrant(new SecurityPrincipalReference("SYSDBA", "User"), new SecurityObjectReference("RDB$ADMIN", "Role"), SecurityPrivilege.FromCode("M"), "SYSDBA", false, SecurityGrantKind.RoleMembership)],
                DateTimeOffset.UtcNow,
                SecurityCacheState.Fresh);
            return Task.FromResult(catalog);
        }

        public SecurityCatalog? GetCachedCatalog() => catalog;

        public void MarkCacheStale()
        {
            if (catalog is not null)
            {
                catalog = catalog with { State = SecurityCacheState.Stale };
            }
        }
    }
}

using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FirebirdAdmin.Application.Connections;
using FirebirdAdmin.Application.Diagnostics;
using FirebirdAdmin.Application.History;
using FirebirdAdmin.Application.Monitoring;
using FirebirdAdmin.Presentation.Wpf.Dashboard;
using FirebirdAdmin.Presentation.Wpf.Diagnostics;
using FirebirdAdmin.Presentation.Wpf.History;
using FirebirdAdmin.Presentation.Wpf.Maintenance;
using FirebirdAdmin.Presentation.Wpf.Metadata;
using FirebirdAdmin.Presentation.Wpf.Monitoring;
using FirebirdAdmin.Presentation.Wpf.Profiler;
using FirebirdAdmin.Presentation.Wpf.Resources;
using FirebirdAdmin.Presentation.Wpf.Security;
using FirebirdAdmin.Presentation.Wpf.Theme;

namespace FirebirdAdmin.Presentation.Wpf.Shell;

public sealed partial class ShellViewModel : ObservableObject
{
    private readonly IConnectionProfileService connectionProfileService;
    private readonly ICredentialStore credentialStore;
    private readonly IFirebirdConnectionService firebirdConnectionService;
    private readonly IMonitoringSessionService monitoringSessionService;
    private readonly IHistoryWriter historyWriter;
    private readonly IDiagnosticEngine diagnosticEngine;
    private readonly IThemeService themeService;
    private CancellationTokenSource? monitoringReadCts;
    private byte[]? activeSessionCredentialBytes;

    [ObservableProperty]
    private string profileName = "Local";

    [ObservableProperty]
    private string host = "localhost";

    [ObservableProperty]
    private int port = 3050;

    [ObservableProperty]
    private string database = string.Empty;

    [ObservableProperty]
    private string userName = "SYSDBA";

    [ObservableProperty]
    private string? charset = "UTF8";

    [ObservableProperty]
    private string? role;

    [ObservableProperty]
    private bool rememberPassword;

    [ObservableProperty]
    private ShellConnectionState connectionState = ShellConnectionState.Disconnected;

    [ObservableProperty]
    private string operationMessage = AppStrings.WorkspacePlaceholder;

    [ObservableProperty]
    private ConnectionContext? activeConnection;

    [ObservableProperty]
    private ShellNavigationItem? selectedNavigationItem;

    [ObservableProperty]
    private ShellWorkspace selectedWorkspace = ShellWorkspace.Dashboard;

    [ObservableProperty]
    private AppTheme currentTheme;

    [ObservableProperty]
    private bool isAboutOpen;

    public ShellViewModel(
        IConnectionProfileService connectionProfileService,
        ICredentialStore credentialStore,
        IFirebirdConnectionService firebirdConnectionService,
        IMonitoringSessionService monitoringSessionService,
        IHistoryWriter historyWriter,
        IDiagnosticEngine diagnosticEngine,
        TransactionsWorkspaceViewModel transactionsWorkspace,
        DashboardViewModel dashboard,
        ProfilerWorkspaceViewModel profilerWorkspace,
        HistoryWorkspaceViewModel historyWorkspace,
        AlertsCenterViewModel alertsCenter,
        MetadataExplorerViewModel metadataExplorer,
        MaintenanceWorkspaceViewModel maintenanceWorkspace,
        SecurityWorkspaceViewModel securityWorkspace,
        IThemeService themeService)
    {
        this.connectionProfileService = connectionProfileService;
        this.credentialStore = credentialStore;
        this.firebirdConnectionService = firebirdConnectionService;
        this.monitoringSessionService = monitoringSessionService;
        this.historyWriter = historyWriter;
        this.diagnosticEngine = diagnosticEngine;
        this.themeService = themeService;
        TransactionsWorkspace = transactionsWorkspace;
        Dashboard = dashboard;
        ProfilerWorkspace = profilerWorkspace;
        HistoryWorkspace = historyWorkspace;
        AlertsCenter = alertsCenter;
        MetadataExplorer = metadataExplorer;
        MaintenanceWorkspace = maintenanceWorkspace;
        SecurityWorkspace = securityWorkspace;
        CurrentTheme = themeService.CurrentTheme;
        ProfilerWorkspace.ProfilerEventReceived += ProfilerWorkspace_OnProfilerEventReceived;

        NavigationItems =
        [
            new(ShellWorkspace.Dashboard, AppStrings.Dashboard, "1", "\uE80F", $"_{AppStrings.Dashboard}"),
            new(ShellWorkspace.Monitoring, AppStrings.Monitoring, "2", "\uE7F4", $"_{AppStrings.Monitoring}"),
            new(ShellWorkspace.SqlProfiler, AppStrings.SqlProfiler, "3", "\uE943", $"_{AppStrings.SqlProfiler}"),
            new(ShellWorkspace.Diagnostics, AppStrings.Diagnostics, "4", "\uE814", $"_{AppStrings.Diagnostics}"),
            new(ShellWorkspace.Metadata, AppStrings.Metadata, "5", "\uE8B7", $"_{AppStrings.Metadata}"),
            new(ShellWorkspace.Security, AppStrings.Security, "6", "\uE72E", $"_{AppStrings.Security}"),
            new(ShellWorkspace.Maintenance, AppStrings.Maintenance, "7", "\uE90F", $"_{AppStrings.Maintenance}"),
            new(ShellWorkspace.History, AppStrings.History, "8", "\uE81C", $"_{AppStrings.History}"),
            new(ShellWorkspace.Settings, AppStrings.Settings, "9", "\uE713", $"_{AppStrings.Settings}")
        ];
        SelectedNavigationItem = NavigationItems[0];
    }

    public string ApplicationName => AppStrings.AppName;
    public string NavigationTitle => AppStrings.NavigationTitle;
    public string ConnectionProfilesTitle => AppStrings.ConnectionProfilesTitle;
    public string DashboardOperationalTitle => AppStrings.DashboardOperationalTitle;
    public string TransactionsTitle => AppStrings.TransactionsTitle;
    public string TransactionDetailsTitle => AppStrings.TransactionDetailsTitle;
    public string TransactionsFilterLabel => AppStrings.TransactionsFilterLabel;
    public string StartLabel => AppStrings.Start;
    public string PauseViewLabel => AppStrings.PauseView;
    public string FollowLabel => AppStrings.Follow;
    public string StopLabel => AppStrings.Stop;
    public string ClearLabel => AppStrings.Clear;
    public string SearchLabel => AppStrings.Search;
    public string RefreshLabel => AppStrings.Refresh;
    public string AcknowledgeLabel => AppStrings.Acknowledge;
    public string ResolveLabel => AppStrings.Resolve;
    public string ReopenLabel => AppStrings.Reopen;
    public string LoadLabel => AppStrings.Load;
    public string RefreshObjectLabel => AppStrings.RefreshObject;
    public string MarkStaleLabel => AppStrings.MarkStale;
    public string ConfirmLabel => AppStrings.Confirm;
    public string ValidateLabel => AppStrings.Validate;
    public string ExecuteLabel => AppStrings.Execute;
    public string ExitLabel => AppStrings.Exit;
    public string AboutLabel => AppStrings.About;
    public string AboutTitle => AppStrings.AboutTitle;
    public string AboutDescription => AppStrings.AboutDescription;
    public string AboutCloseLabel => AppStrings.AboutClose;
    public string AboutVersionText => string.Format(AppStrings.AboutVersionFormat, GetInformationalVersion());
    public string TitleBarVersionText => GetInformationalVersion();
    public string CancelLabel => AppStrings.Cancel;
    public string UpdateHistoryLabel => AppStrings.UpdateHistory;
    public string AlertsInstruction => AppStrings.AlertsInstruction;
    public string KeyboardHelp => AppStrings.KeyboardHelp;
    public string NameLabel => AppStrings.Name;
    public string HostLabel => AppStrings.Host;
    public string PortLabel => AppStrings.Port;
    public string DatabaseLabel => AppStrings.Database;
    public string SelectDatabaseLabel => AppStrings.SelectDatabase;
    public string UserNameLabel => AppStrings.UserName;
    public string PasswordLabel => AppStrings.Password;
    public string RoleLabel => AppStrings.Role;
    public string RememberPasswordLabel => AppStrings.RememberPassword;
    public string SaveProfileLabel => AppStrings.SaveProfile;
    public string TestConnectionLabel => AppStrings.TestConnection;
    public string ConnectLabel => AppStrings.Connect;
    public string ThemeToggleLabel => string.Format(
        AppStrings.ThemeToggleFormat,
        CurrentTheme == AppTheme.Light ? AppStrings.ThemeLight : AppStrings.ThemeDark);
    public string TraceStatus => ProfilerWorkspace.State switch
    {
        Application.Profiler.ProfilerState.Running => "Trace em execução",
        Application.Profiler.ProfilerState.Starting => "Trace iniciando",
        Application.Profiler.ProfilerState.PausedView => "Trace capturando, visual pausada",
        Application.Profiler.ProfilerState.Stopping => "Trace encerrando",
        Application.Profiler.ProfilerState.Failed => "Trace com falha",
        _ => AppStrings.TraceStopped
    };
    public string PollingStatus => AppStrings.PollingStopped;
    public string WorkspaceTitle => SelectedNavigationItem?.Title ?? AppStrings.Dashboard;
    public bool IsNavigationExpanded => true;
    public bool HasActiveConnection => ActiveConnection is not null;
    public bool IsTraceRunning => ProfilerWorkspace.State is Application.Profiler.ProfilerState.Running;
    public bool IsPollingRunning => false;
    public ObservableCollection<ShellNavigationItem> NavigationItems { get; }
    public DashboardViewModel Dashboard { get; }
    public TransactionsWorkspaceViewModel TransactionsWorkspace { get; }
    public ProfilerWorkspaceViewModel ProfilerWorkspace { get; }
    public HistoryWorkspaceViewModel HistoryWorkspace { get; }
    public AlertsCenterViewModel AlertsCenter { get; }
    public MetadataExplorerViewModel MetadataExplorer { get; }
    public MaintenanceWorkspaceViewModel MaintenanceWorkspace { get; }
    public SecurityWorkspaceViewModel SecurityWorkspace { get; }

    public string ConnectionContext => ActiveConnection is null
        ? AppStrings.ConnectionContextEmpty
        : $"{ActiveConnection.ProfileName} | {ActiveConnection.Host}:{ActiveConnection.Port} | {ActiveConnection.Database} | Firebird {ActiveConnection.ServerVersion.Raw}";

    public string ReadyStatus => ConnectionState switch
    {
        ShellConnectionState.Disconnected => AppStrings.ReadyStatus,
        ShellConnectionState.Connecting => AppStrings.ConnectingStatus,
        ShellConnectionState.Connected => AppStrings.ConnectedStatus,
        ShellConnectionState.ConnectionFailed => AppStrings.ConnectionFailed,
        _ => AppStrings.ReadyStatus
    };

    public string WorkspacePlaceholder => ActiveConnection is null
        ? OperationMessage
        : $"{ActiveConnection.Capabilities.Explanation} Toolsets: {ActiveConnection.Toolset.Candidates.Count(candidate => candidate.IsAvailable)} encontrados.";

    partial void OnSelectedNavigationItemChanged(ShellNavigationItem? value)
    {
        if (value is null)
        {
            return;
        }

        if (SelectedWorkspace != value.Workspace)
        {
            SelectedWorkspace = value.Workspace;
        }

        OnPropertyChanged(nameof(WorkspaceTitle));
    }

    partial void OnSelectedWorkspaceChanged(ShellWorkspace value)
    {
        if (SelectedNavigationItem?.Workspace != value)
        {
            SelectedNavigationItem = NavigationItems.First(item => item.Workspace == value);
        }

        OnPropertyChanged(nameof(WorkspaceTitle));
    }

    partial void OnActiveConnectionChanged(ConnectionContext? value)
    {
        OnPropertyChanged(nameof(HasActiveConnection));
        OnPropertyChanged(nameof(ConnectionContext));
        OnPropertyChanged(nameof(WorkspacePlaceholder));
    }

    partial void OnConnectionStateChanged(ShellConnectionState value)
    {
        OnPropertyChanged(nameof(ReadyStatus));
    }

    partial void OnCurrentThemeChanged(AppTheme value)
    {
        OnPropertyChanged(nameof(ThemeToggleLabel));
    }

    partial void OnOperationMessageChanged(string value)
    {
        OnPropertyChanged(nameof(WorkspacePlaceholder));
    }

    public async Task SaveProfileAsync(string password, CancellationToken cancellationToken = default)
    {
        using var secret = string.IsNullOrEmpty(password) ? null : CredentialSecret.FromPlainText(password);
        await connectionProfileService.SaveAsync(CreateProfileRequest(secret), cancellationToken);
        OperationMessage = "Perfil salvo.";
    }

    public async Task TestConnectionAsync(string password, CancellationToken cancellationToken = default)
    {
        await ConnectCoreAsync(password, setActiveConnection: false, cancellationToken);
    }

    public async Task ConnectAsync(string password, CancellationToken cancellationToken = default)
    {
        await ConnectCoreAsync(password, setActiveConnection: true, cancellationToken);
    }

    [RelayCommand]
    public void SelectWorkspace(ShellWorkspace workspace)
    {
        SelectedWorkspace = workspace;
    }

    [RelayCommand]
    public void SelectWorkspaceByShortcut(string? shortcut)
    {
        if (!int.TryParse(shortcut, out var index) || index < 1 || index > NavigationItems.Count)
        {
            return;
        }

        SelectedNavigationItem = NavigationItems[index - 1];
    }

    [RelayCommand]
    public async Task RefreshSelectedWorkspaceAsync(CancellationToken cancellationToken = default)
    {
        switch (SelectedWorkspace)
        {
            case ShellWorkspace.History:
                await HistoryWorkspace.SearchAsync(cancellationToken);
                break;
            case ShellWorkspace.Diagnostics:
                await AlertsCenter.LoadAsync(cancellationToken);
                break;
            case ShellWorkspace.Metadata:
                await MetadataExplorer.RefreshCatalogAsync(cancellationToken);
                break;
            case ShellWorkspace.Security:
                await SecurityWorkspace.RefreshAsync(cancellationToken);
                break;
            case ShellWorkspace.Maintenance:
                await MaintenanceWorkspace.LoadHistoryAsync(cancellationToken);
                break;
        }
    }

    [RelayCommand]
    public void CancelCurrentWorkspaceAction()
    {
        if (SelectedWorkspace == ShellWorkspace.Maintenance)
        {
            MaintenanceWorkspace.Cancel();
        }
    }

    [RelayCommand]
    public void ToggleTheme()
    {
        CurrentTheme = themeService.Toggle();
    }

    [RelayCommand]
    public void ShowAbout()
    {
        IsAboutOpen = true;
    }

    [RelayCommand]
    public void CloseAbout()
    {
        IsAboutOpen = false;
    }

    private static string GetInformationalVersion()
    {
        return typeof(ShellViewModel).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
            ?? typeof(ShellViewModel).Assembly.GetName().Version?.ToString()
            ?? "1.0.0";
    }

    private async Task ConnectCoreAsync(string password, bool setActiveConnection, CancellationToken cancellationToken)
    {
        ConnectionState = ShellConnectionState.Connecting;
        OperationMessage = AppStrings.ConnectingStatus;

        byte[]? credentialBytes = null;
        try
        {
            using var profileSecret = string.IsNullOrEmpty(password) ? null : CredentialSecret.FromPlainText(password);
            var profile = await connectionProfileService.SaveAsync(CreateProfileRequest(profileSecret), cancellationToken);
            using var savedSecret = profileSecret is null ? await credentialStore.TryLoadAsync(profile.Id, cancellationToken) : null;

            credentialBytes = !string.IsNullOrEmpty(password)
                ? Encoding.UTF8.GetBytes(password)
                : savedSecret?.CopyBytes();

            using var connectionSecret = CreateSecretCopy(credentialBytes);
            var context = await firebirdConnectionService.ConnectAsync(
                new ConnectionRequest(profile, connectionSecret),
                cancellationToken);

            if (setActiveConnection)
            {
                StoreActiveSessionCredential(credentialBytes);
                ActiveConnection = context;
                ProfilerWorkspace.SetReady();
                using var metadataSecret = CreateSecretCopy(credentialBytes);
                using var maintenanceSecret = CreateSecretCopy(credentialBytes);
                using var securitySecret = CreateSecretCopy(credentialBytes);
                using var monitoringSecret = CreateSecretCopy(credentialBytes);
                MetadataExplorer.SetConnection(context, metadataSecret);
                MaintenanceWorkspace.SetConnection(context, maintenanceSecret);
                SecurityWorkspace.SetConnection(context, securitySecret);
                _ = MetadataExplorer.LoadCatalogAsync();
                _ = MaintenanceWorkspace.LoadHistoryAsync();
                _ = SecurityWorkspace.LoadAsync();
                await StartMonitoringAsync(profile, monitoringSecret, context, cancellationToken);
            }

            ConnectionState = ShellConnectionState.Connected;
            OperationMessage = setActiveConnection ? AppStrings.ConnectedStatus : AppStrings.TestConnectionSucceeded;
        }
        catch (Exception ex)
        {
            ConnectionState = ShellConnectionState.ConnectionFailed;
            OperationMessage = ex.Message;
        }
        finally
        {
            if (credentialBytes is not null)
            {
                Array.Clear(credentialBytes);
            }
        }
    }

    public async Task StartProfilerAsync(string password, CancellationToken cancellationToken = default)
    {
        if (ActiveConnection is null)
        {
            ProfilerWorkspace.SetFailed("Conecte a um banco antes de iniciar o SQL Profiler.");
            return;
        }

        using var providedSecret = string.IsNullOrEmpty(password) ? null : CredentialSecret.FromPlainText(password);
        using var sessionSecret = providedSecret is null ? CreateSecretCopy(activeSessionCredentialBytes) : null;
        using var savedSecret = providedSecret is null && sessionSecret is null ? await credentialStore.TryLoadAsync(ActiveConnection.ProfileId, cancellationToken) : null;
        await ProfilerWorkspace.StartAsync(ActiveConnection, providedSecret ?? sessionSecret ?? savedSecret, cancellationToken);
        OnPropertyChanged(nameof(TraceStatus));
        OnPropertyChanged(nameof(IsTraceRunning));
    }

    public async Task StopProfilerAsync(CancellationToken cancellationToken = default)
    {
        await ProfilerWorkspace.StopAsync(cancellationToken);
        OnPropertyChanged(nameof(TraceStatus));
        OnPropertyChanged(nameof(IsTraceRunning));
    }

    public void PauseProfilerView()
    {
        ProfilerWorkspace.PauseView();
        OnPropertyChanged(nameof(TraceStatus));
    }

    public void ResumeProfilerFollow()
    {
        ProfilerWorkspace.ResumeFollow();
        OnPropertyChanged(nameof(TraceStatus));
    }

    private async Task StartMonitoringAsync(
        ConnectionProfile profile,
        CredentialSecret? password,
        ConnectionContext context,
        CancellationToken cancellationToken)
    {
        TransactionsWorkspace.SetLoading();
        await monitoringSessionService.StartAsync(context, profile, password, PollingOptions.Normal, cancellationToken);

        await (monitoringReadCts?.CancelAsync() ?? Task.CompletedTask);
        monitoringReadCts = new CancellationTokenSource();
        _ = ReadMonitoringSnapshotsAsync(monitoringReadCts.Token);
    }

    private async Task ReadMonitoringSnapshotsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var snapshot in monitoringSessionService.ReadAllAsync(cancellationToken))
            {
                Dashboard.ApplySnapshot(snapshot);
                TransactionsWorkspace.ApplySnapshot(snapshot);
                _ = RunDiagnosticsAsync(snapshot);
                _ = PersistMonitoringSnapshotAsync(snapshot);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Dashboard.SetError(ex.Message);
            TransactionsWorkspace.SetError(ex.Message);
        }
    }

    private ConnectionProfileRequest CreateProfileRequest(CredentialSecret? secret)
    {
        return new ConnectionProfileRequest(
            Id: null,
            ProfileName,
            Host,
            Port,
            Database,
            UserName,
            Charset,
            Role,
            RememberPassword,
            secret);
    }

    private static CredentialSecret? CreateSecretCopy(byte[]? bytes)
    {
        return bytes is null ? null : CredentialSecret.FromBytes(bytes);
    }

    private void StoreActiveSessionCredential(byte[]? bytes)
    {
        ClearActiveSessionCredential();
        activeSessionCredentialBytes = bytes is null ? null : bytes.ToArray();
    }

    private void ClearActiveSessionCredential()
    {
        if (activeSessionCredentialBytes is not null)
        {
            Array.Clear(activeSessionCredentialBytes);
            activeSessionCredentialBytes = null;
        }
    }

    private async Task PersistMonitoringSnapshotAsync(MonitoringSnapshot snapshot)
    {
        try
        {
            await historyWriter.WriteMonitoringSnapshotsAsync(ActiveConnection?.ProfileId, [snapshot], CancellationToken.None);
        }
        catch (Exception ex)
        {
            OperationMessage = $"Falha ao persistir histórico MON$: {ex.Message}";
        }
    }

    private async void ProfilerWorkspace_OnProfilerEventReceived(object? sender, Application.Profiler.ProfilerEvent profilerEvent)
    {
        await RunDiagnosticsAsync(profilerEvent);
    }

    private async Task RunDiagnosticsAsync(MonitoringSnapshot snapshot)
    {
        var results = diagnosticEngine.Evaluate(snapshot, ActiveConnection?.ProfileId);
        await AlertsCenter.AcceptDiagnosticResultsAsync(results);
    }

    private async Task RunDiagnosticsAsync(Application.Profiler.ProfilerEvent profilerEvent)
    {
        var results = diagnosticEngine.Evaluate(profilerEvent, ActiveConnection?.ProfileId);
        await AlertsCenter.AcceptDiagnosticResultsAsync(results);
    }
}

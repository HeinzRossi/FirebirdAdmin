using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
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

namespace FirebirdAdmin.Presentation.Wpf.Shell;

public sealed partial class ShellViewModel : ObservableObject
{
    private readonly IConnectionProfileService connectionProfileService;
    private readonly ICredentialStore credentialStore;
    private readonly IFirebirdConnectionService firebirdConnectionService;
    private readonly IMonitoringSessionService monitoringSessionService;
    private readonly IHistoryWriter historyWriter;
    private readonly IDiagnosticEngine diagnosticEngine;
    private CancellationTokenSource? monitoringReadCts;

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
        SecurityWorkspaceViewModel securityWorkspace)
    {
        this.connectionProfileService = connectionProfileService;
        this.credentialStore = credentialStore;
        this.firebirdConnectionService = firebirdConnectionService;
        this.monitoringSessionService = monitoringSessionService;
        this.historyWriter = historyWriter;
        this.diagnosticEngine = diagnosticEngine;
        TransactionsWorkspace = transactionsWorkspace;
        Dashboard = dashboard;
        ProfilerWorkspace = profilerWorkspace;
        HistoryWorkspace = historyWorkspace;
        AlertsCenter = alertsCenter;
        MetadataExplorer = metadataExplorer;
        MaintenanceWorkspace = maintenanceWorkspace;
        SecurityWorkspace = securityWorkspace;
        ProfilerWorkspace.ProfilerEventReceived += ProfilerWorkspace_OnProfilerEventReceived;

        NavigationItems =
        [
            new(AppStrings.Dashboard),
            new(AppStrings.Monitoring),
            new(AppStrings.SqlProfiler),
            new(AppStrings.Diagnostics),
            new(AppStrings.Metadata),
            new("Segurança"),
            new(AppStrings.Maintenance),
            new(AppStrings.History),
            new(AppStrings.Settings)
        ];
    }

    public string ApplicationName => AppStrings.AppName;
    public string NavigationTitle => AppStrings.NavigationTitle;
    public string ConnectionProfilesTitle => AppStrings.ConnectionProfilesTitle;
    public string NameLabel => AppStrings.Name;
    public string HostLabel => AppStrings.Host;
    public string PortLabel => AppStrings.Port;
    public string DatabaseLabel => AppStrings.Database;
    public string UserNameLabel => AppStrings.UserName;
    public string PasswordLabel => AppStrings.Password;
    public string RoleLabel => AppStrings.Role;
    public string RememberPasswordLabel => AppStrings.RememberPassword;
    public string SaveProfileLabel => AppStrings.SaveProfile;
    public string TestConnectionLabel => AppStrings.TestConnection;
    public string ConnectLabel => AppStrings.Connect;
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
    public string WorkspaceTitle => AppStrings.Dashboard;
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

    private async Task ConnectCoreAsync(string password, bool setActiveConnection, CancellationToken cancellationToken)
    {
        ConnectionState = ShellConnectionState.Connecting;
        OperationMessage = AppStrings.ConnectingStatus;

        try
        {
            using var providedSecret = string.IsNullOrEmpty(password) ? null : CredentialSecret.FromPlainText(password);
            var profile = await connectionProfileService.SaveAsync(CreateProfileRequest(providedSecret), cancellationToken);
            using var savedSecret = providedSecret is null ? await credentialStore.TryLoadAsync(profile.Id, cancellationToken) : null;

            var context = await firebirdConnectionService.ConnectAsync(
                new ConnectionRequest(profile, providedSecret ?? savedSecret),
                cancellationToken);

            if (setActiveConnection)
            {
                ActiveConnection = context;
                ProfilerWorkspace.SetReady();
                MetadataExplorer.SetConnection(context, providedSecret ?? savedSecret);
                MaintenanceWorkspace.SetConnection(context, providedSecret ?? savedSecret);
                SecurityWorkspace.SetConnection(context, providedSecret ?? savedSecret);
                _ = MetadataExplorer.LoadCatalogAsync();
                _ = MaintenanceWorkspace.LoadHistoryAsync();
                _ = SecurityWorkspace.LoadAsync();
                await StartMonitoringAsync(profile, providedSecret ?? savedSecret, context, cancellationToken);
            }

            ConnectionState = ShellConnectionState.Connected;
            OperationMessage = setActiveConnection ? AppStrings.ConnectedStatus : "Teste de conexão concluído.";
        }
        catch (Exception ex)
        {
            ConnectionState = ShellConnectionState.ConnectionFailed;
            OperationMessage = ex.Message;
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
        using var savedSecret = providedSecret is null ? await credentialStore.TryLoadAsync(ActiveConnection.ProfileId, cancellationToken) : null;
        await ProfilerWorkspace.StartAsync(ActiveConnection, providedSecret ?? savedSecret, cancellationToken);
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

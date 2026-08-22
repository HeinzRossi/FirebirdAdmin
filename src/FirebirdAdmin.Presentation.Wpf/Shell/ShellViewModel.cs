using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FirebirdAdmin.Application.Connections;
using FirebirdAdmin.Application.Monitoring;
using FirebirdAdmin.Presentation.Wpf.Dashboard;
using FirebirdAdmin.Presentation.Wpf.Monitoring;
using FirebirdAdmin.Presentation.Wpf.Resources;

namespace FirebirdAdmin.Presentation.Wpf.Shell;

public sealed partial class ShellViewModel : ObservableObject
{
    private readonly IConnectionProfileService connectionProfileService;
    private readonly ICredentialStore credentialStore;
    private readonly IFirebirdConnectionService firebirdConnectionService;
    private readonly IMonitoringSessionService monitoringSessionService;
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
        TransactionsWorkspaceViewModel transactionsWorkspace,
        DashboardViewModel dashboard)
    {
        this.connectionProfileService = connectionProfileService;
        this.credentialStore = credentialStore;
        this.firebirdConnectionService = firebirdConnectionService;
        this.monitoringSessionService = monitoringSessionService;
        TransactionsWorkspace = transactionsWorkspace;
        Dashboard = dashboard;

        NavigationItems =
        [
            new(AppStrings.Dashboard),
            new(AppStrings.Monitoring),
            new(AppStrings.SqlProfiler),
            new(AppStrings.Diagnostics),
            new(AppStrings.Metadata),
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
    public string TraceStatus => AppStrings.TraceStopped;
    public string PollingStatus => AppStrings.PollingStopped;
    public string WorkspaceTitle => AppStrings.Dashboard;
    public bool IsNavigationExpanded => true;
    public bool HasActiveConnection => ActiveConnection is not null;
    public bool IsTraceRunning => false;
    public bool IsPollingRunning => false;
    public ObservableCollection<ShellNavigationItem> NavigationItems { get; }
    public DashboardViewModel Dashboard { get; }
    public TransactionsWorkspaceViewModel TransactionsWorkspace { get; }

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
}

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FirebirdAdmin.Application.Connections;
using FirebirdAdmin.Application.Maintenance;

namespace FirebirdAdmin.Presentation.Wpf.Maintenance;

public sealed partial class MaintenanceWorkspaceViewModel(
    IMaintenanceService maintenanceService,
    IMaintenanceHistoryStore historyStore) : ObservableObject, IDisposable
{
    private ConnectionContext? activeConnection;
    private CredentialSecret? password;
    private CancellationTokenSource? executionCts;
    private bool disposed;

    [ObservableProperty]
    private string operationType = MaintenanceOperationType.Backup.ToString();

    [ObservableProperty]
    private string sourcePath = string.Empty;

    [ObservableProperty]
    private string targetPath = string.Empty;

    [ObservableProperty]
    private bool confirmed;

    [ObservableProperty]
    private bool isRunning;

    [ObservableProperty]
    private string stage = "Configurar";

    [ObservableProperty]
    private string message = "Conecte a um banco para validar manutenção.";

    [ObservableProperty]
    private string preflightText = "-";

    [ObservableProperty]
    private string progressText = "Aguardando.";

    [ObservableProperty]
    private MaintenanceOperationRowViewModel? selectedHistory;

    public ObservableCollection<string> Logs { get; } = [];
    public ObservableCollection<MaintenanceOperationRowViewModel> History { get; } = [];
    public string ToolsetText => activeConnection is null
        ? "Sem conexão."
        : string.Join(" | ", activeConnection.Toolset.Candidates.Where(candidate => candidate.IsAvailable).Select(candidate => $"{candidate.Kind}: {candidate.Path}"));
    public bool CanExecute => activeConnection is not null && Confirmed && !IsRunning;
    public string SelectedHistoryDetails => SelectedHistory is null
        ? "-"
        : $"{SelectedHistory.Type} {SelectedHistory.Status}{Environment.NewLine}{SelectedHistory.Source}{Environment.NewLine}{SelectedHistory.Target}{Environment.NewLine}{SelectedHistory.Message}";

    public void SetConnection(ConnectionContext connection, CredentialSecret? credential)
    {
        activeConnection = connection;
        password?.Dispose();
        password = credential is null ? null : CredentialSecret.FromBytes(credential.CopyBytes());
        SourcePath = connection.Database;
        if (string.IsNullOrWhiteSpace(TargetPath))
        {
            TargetPath = $"{connection.Database}.fbk";
        }

        Message = "Manutenção pronta para preflight.";
        OnPropertyChanged(nameof(ToolsetText));
        OnPropertyChanged(nameof(CanExecute));
    }

    public async Task ValidateAsync(CancellationToken cancellationToken = default)
    {
        var request = CreateRequest();
        if (request is null)
        {
            Message = "Conexão ativa obrigatória.";
            return;
        }

        Stage = "Validar";
        var result = await maintenanceService.ValidateAsync(request, cancellationToken);
        PreflightText = FormatPreflight(result);
        Message = result.CanExecute ? "Preflight válido. Revise e confirme antes de executar." : "Preflight bloqueado.";
        OnPropertyChanged(nameof(CanExecute));
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var request = CreateRequest();
        if (request is null)
        {
            Message = "Conexão ativa obrigatória.";
            return;
        }

        executionCts?.Dispose();
        executionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Logs.Clear();
        IsRunning = true;
        Stage = "Executar";
        Message = "Executando manutenção.";
        OnPropertyChanged(nameof(CanExecute));

        void OnProgress(object? sender, MaintenanceProgress progress)
        {
            ProgressText = progress.Percent is null
                ? $"{progress.Stage}: {progress.Message}"
                : $"{progress.Stage}: {progress.Percent:P0} - {progress.Message}";
        }

        void OnLog(object? sender, MaintenanceLogLine line)
        {
            Logs.Add($"[{line.Stream}] {line.Text}");
        }

        maintenanceService.ProgressChanged += OnProgress;
        maintenanceService.LogReceived += OnLog;
        try
        {
            var result = await maintenanceService.ExecuteAsync(request, password, executionCts.Token);
            Message = result.Operation.Message;
            Stage = result.Operation.Status.ToString();
            await LoadHistoryAsync(CancellationToken.None);
        }
        finally
        {
            maintenanceService.ProgressChanged -= OnProgress;
            maintenanceService.LogReceived -= OnLog;
            IsRunning = false;
            OnPropertyChanged(nameof(CanExecute));
        }
    }

    public void Cancel()
    {
        executionCts?.Cancel();
    }

    public async Task LoadHistoryAsync(CancellationToken cancellationToken = default)
    {
        var operations = await historyStore.ListRecentAsync(25, cancellationToken);
        History.Clear();
        foreach (var operation in operations)
        {
            History.Add(new MaintenanceOperationRowViewModel(operation));
        }
    }

    partial void OnConfirmedChanged(bool value)
    {
        OnPropertyChanged(nameof(CanExecute));
    }

    partial void OnIsRunningChanged(bool value)
    {
        OnPropertyChanged(nameof(CanExecute));
    }

    partial void OnSelectedHistoryChanged(MaintenanceOperationRowViewModel? value)
    {
        OnPropertyChanged(nameof(SelectedHistoryDetails));
    }

    private MaintenanceRequest? CreateRequest()
    {
        if (activeConnection is null)
        {
            return null;
        }

        return Enum.Parse<MaintenanceOperationType>(OperationType) switch
        {
            MaintenanceOperationType.Backup => new BackupRequest(activeConnection, SourcePath, TargetPath, Confirmed),
            MaintenanceOperationType.Restore => new RestoreRequest(activeConnection, SourcePath, TargetPath, Confirmed),
            MaintenanceOperationType.Validation => new ValidationRequest(activeConnection, SourcePath, Confirmed),
            MaintenanceOperationType.Sweep => new SweepRequest(activeConnection, SourcePath, Confirmed),
            _ => null
        };
    }

    private static string FormatPreflight(MaintenancePreflightResult result)
    {
        return string.Join(
            Environment.NewLine,
            result.Errors.Select(error => $"ERRO: {error}")
                .Concat(result.Warnings.Select(warning => $"AVISO: {warning}"))
                .Concat(result.ReviewLines.Select(line => $"REVISÃO: {line}")));
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        executionCts?.Dispose();
        password?.Dispose();
        disposed = true;
    }
}

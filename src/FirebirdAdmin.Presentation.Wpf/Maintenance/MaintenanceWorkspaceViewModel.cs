using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using FirebirdAdmin.Application.Connections;
using FirebirdAdmin.Application.Maintenance;
using FirebirdAdmin.Presentation.Wpf.Diagnostics;
using FirebirdAdmin.Presentation.Wpf.Resources;

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
    private double progressValue;

    [ObservableProperty]
    private bool isProgressIndeterminate;

    [ObservableProperty]
    private bool isProgressVisible = true;

    [ObservableProperty]
    private string progressStatusText = AppStrings.MaintenanceProgressWaiting;

    [ObservableProperty]
    private MaintenanceOperationRowViewModel? selectedHistory;

    public ObservableCollection<string> Logs { get; } = [];
    public ObservableCollection<MaintenanceOperationRowViewModel> History { get; } = [];
    public IReadOnlyList<FilterOption> OperationTypeOptions { get; } =
    [
        new(AppStrings.MaintenanceOperationBackup, MaintenanceOperationType.Backup.ToString()),
        new(AppStrings.MaintenanceOperationRestore, MaintenanceOperationType.Restore.ToString()),
        new(AppStrings.MaintenanceOperationValidation, MaintenanceOperationType.Validation.ToString()),
        new(AppStrings.MaintenanceOperationSweep, MaintenanceOperationType.Sweep.ToString())
    ];

    public string ToolsetText => activeConnection is null
        ? "Sem conexão."
        : string.Join(" | ", activeConnection.Toolset.Candidates.Where(candidate => candidate.IsAvailable).Select(candidate => $"{candidate.Kind}: {candidate.Path}"));
    public bool CanExecute => activeConnection is not null && Confirmed && !IsRunning;
    public bool IsTargetPathEnabled => ParseOperationType() is MaintenanceOperationType.Backup or MaintenanceOperationType.Restore;
    public string SourcePathLabel => ParseOperationType() switch
    {
        MaintenanceOperationType.Restore => AppStrings.MaintenanceSourceBackup,
        MaintenanceOperationType.Validation => AppStrings.MaintenanceSourceDatabase,
        MaintenanceOperationType.Sweep => AppStrings.MaintenanceSourceDatabase,
        _ => AppStrings.MaintenanceSourceDatabase
    };

    public string TargetPathLabel => ParseOperationType() switch
    {
        MaintenanceOperationType.Restore => AppStrings.MaintenanceTargetNewDatabase,
        MaintenanceOperationType.Backup => AppStrings.MaintenanceTargetBackup,
        _ => AppStrings.MaintenanceTargetNotUsed
    };

    public string SelectedHistoryDetails => SelectedHistory is null
        ? "-"
        : $"{SelectedHistory.Type} {SelectedHistory.Status}{Environment.NewLine}{SelectedHistory.Source}{Environment.NewLine}{SelectedHistory.Target}{Environment.NewLine}{SelectedHistory.Message}";

    public void SetConnection(ConnectionContext connection, CredentialSecret? credential)
    {
        activeConnection = connection;
        password?.Dispose();
        password = credential is null ? null : CredentialSecret.FromBytes(credential.CopyBytes());
        ApplyOperationDefaults(force: true);

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
        ProgressValue = 0;
        IsProgressVisible = true;
        IsProgressIndeterminate = true;
        ProgressText = AppStrings.MaintenanceProgressRunning;
        ProgressStatusText = AppStrings.MaintenanceProgressRunning;
        OnPropertyChanged(nameof(CanExecute));

        void OnProgress(object? sender, MaintenanceProgress progress)
        {
            ProgressText = progress.Percent is null
                ? $"{progress.Stage}: {progress.Message}"
                : $"{progress.Stage}: {progress.Percent:P0} - {progress.Message}";
            ProgressStatusText = ProgressText;

            if (progress.Percent is null)
            {
                IsProgressIndeterminate = IsRunning;
                return;
            }

            IsProgressIndeterminate = false;
            ProgressValue = Math.Clamp(progress.Percent.Value * 100, 0, 100);
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
            Stage = FormatStatus(result.Operation.Status);
            ApplyFinalProgressState(result.Operation.Status);
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

    private void ApplyFinalProgressState(MaintenanceOperationStatus status)
    {
        IsProgressVisible = true;
        IsProgressIndeterminate = false;

        switch (status)
        {
            case MaintenanceOperationStatus.Succeeded:
                ProgressValue = 100;
                ProgressStatusText = AppStrings.MaintenanceProgressCompleted;
                ProgressText = AppStrings.MaintenanceProgressCompleted;
                break;
            case MaintenanceOperationStatus.Cancelled:
                ProgressStatusText = AppStrings.MaintenanceProgressCancelled;
                ProgressText = AppStrings.MaintenanceProgressCancelled;
                break;
            case MaintenanceOperationStatus.Failed:
                ProgressStatusText = AppStrings.MaintenanceProgressFailed;
                ProgressText = AppStrings.MaintenanceProgressFailed;
                break;
            default:
                ProgressStatusText = ProgressText;
                break;
        }
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

    partial void OnOperationTypeChanged(string value)
    {
        ApplyOperationDefaults(force: false);
        OnPropertyChanged(nameof(IsTargetPathEnabled));
        OnPropertyChanged(nameof(SourcePathLabel));
        OnPropertyChanged(nameof(TargetPathLabel));
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

        return ParseOperationType() switch
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

    private MaintenanceOperationType ParseOperationType()
    {
        return Enum.TryParse<MaintenanceOperationType>(OperationType, ignoreCase: true, out var type)
            ? type
            : MaintenanceOperationType.Backup;
    }

    private void ApplyOperationDefaults(bool force)
    {
        if (activeConnection is null)
        {
            return;
        }

        var databasePath = activeConnection.Database;
        var backupPath = $"{databasePath}.fbk";
        switch (ParseOperationType())
        {
            case MaintenanceOperationType.Restore:
                SourcePath = force || string.IsNullOrWhiteSpace(SourcePath) || !SourcePath.EndsWith(".fbk", StringComparison.OrdinalIgnoreCase)
                    ? backupPath
                    : SourcePath;
                TargetPath = CreateRestoreTargetPath(databasePath);
                break;
            case MaintenanceOperationType.Validation:
            case MaintenanceOperationType.Sweep:
                SourcePath = databasePath;
                TargetPath = string.Empty;
                break;
            default:
                SourcePath = databasePath;
                TargetPath = backupPath;
                break;
        }
    }

    private static string CreateRestoreTargetPath(string databasePath)
    {
        var directory = Path.GetDirectoryName(databasePath);
        var name = Path.GetFileNameWithoutExtension(databasePath);
        var candidateDirectory = string.IsNullOrWhiteSpace(directory) ? string.Empty : directory;
        var candidate = Path.Combine(candidateDirectory, $"{name}-restored.fdb");
        var index = 1;
        while (File.Exists(candidate))
        {
            candidate = Path.Combine(candidateDirectory, $"{name}-restored-{index}.fdb");
            index++;
        }

        return candidate;
    }

    private static string FormatStatus(MaintenanceOperationStatus status)
    {
        return status switch
        {
            MaintenanceOperationStatus.Pending => "Pendente",
            MaintenanceOperationStatus.Running => "Executando",
            MaintenanceOperationStatus.Succeeded => "Concluída",
            MaintenanceOperationStatus.Failed => "Falhou",
            MaintenanceOperationStatus.Cancelled => "Cancelada",
            _ => status.ToString()
        };
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

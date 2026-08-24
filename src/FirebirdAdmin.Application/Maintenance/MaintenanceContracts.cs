using FirebirdAdmin.Application.Connections;

namespace FirebirdAdmin.Application.Maintenance;

public interface IMaintenancePreflightService
{
    Task<MaintenancePreflightResult> ValidateAsync(MaintenanceRequest request, CancellationToken cancellationToken);
}

public interface IMaintenanceService
{
    MaintenanceOperation? ActiveOperation { get; }
    event EventHandler<MaintenanceProgress>? ProgressChanged;
    event EventHandler<MaintenanceLogLine>? LogReceived;
    Task<MaintenancePreflightResult> ValidateAsync(MaintenanceRequest request, CancellationToken cancellationToken);
    Task<MaintenanceResult> ExecuteAsync(MaintenanceRequest request, CredentialSecret? password, CancellationToken cancellationToken);
}

public interface IFirebirdToolRunner
{
    Task<ToolExecutionResult> ExecuteAsync(
        Guid operationId,
        FirebirdToolCommand command,
        IProgress<MaintenanceLogLine> progress,
        CancellationToken cancellationToken);
}

public interface IMaintenanceHistoryStore
{
    Task SaveOperationAsync(MaintenanceOperation operation, CancellationToken cancellationToken);
    Task SaveLogAsync(MaintenanceLogLine logLine, CancellationToken cancellationToken);
    Task<IReadOnlyList<MaintenanceOperation>> ListRecentAsync(int take, CancellationToken cancellationToken);
}

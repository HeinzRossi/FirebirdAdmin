using FirebirdAdmin.Application.Connections;

namespace FirebirdAdmin.Application.Maintenance;

public sealed record MaintenanceOperation(
    Guid Id,
    Guid? ConnectionProfileId,
    MaintenanceOperationType Type,
    MaintenanceOperationStatus Status,
    string Source,
    string? Target,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    int ExitCode,
    string Message);

public sealed record MaintenancePreflightResult(
    bool CanExecute,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> ReviewLines);

public abstract record MaintenanceRequest(
    MaintenanceOperationType Type,
    ConnectionContext Connection,
    string Source,
    string? Target,
    bool Confirmed);

public sealed record BackupRequest(
    ConnectionContext Connection,
    string DatabasePath,
    string BackupPath,
    bool Confirmed)
    : MaintenanceRequest(MaintenanceOperationType.Backup, Connection, DatabasePath, BackupPath, Confirmed);

public sealed record RestoreRequest(
    ConnectionContext Connection,
    string BackupPath,
    string RestoreDatabasePath,
    bool Confirmed)
    : MaintenanceRequest(MaintenanceOperationType.Restore, Connection, BackupPath, RestoreDatabasePath, Confirmed);

public sealed record ValidationRequest(
    ConnectionContext Connection,
    string DatabasePath,
    bool Confirmed)
    : MaintenanceRequest(MaintenanceOperationType.Validation, Connection, DatabasePath, null, Confirmed);

public sealed record SweepRequest(
    ConnectionContext Connection,
    string DatabasePath,
    bool Confirmed)
    : MaintenanceRequest(MaintenanceOperationType.Sweep, Connection, DatabasePath, null, Confirmed);

public sealed record MaintenanceProgress(
    Guid OperationId,
    string Stage,
    double? Percent,
    string Message,
    DateTimeOffset Timestamp);

public sealed record MaintenanceLogLine(
    Guid OperationId,
    DateTimeOffset Timestamp,
    string Stream,
    string Text);

public sealed record MaintenanceResult(
    MaintenanceOperation Operation,
    IReadOnlyList<MaintenanceLogLine> Logs);

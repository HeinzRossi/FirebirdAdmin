using FirebirdAdmin.Application.Maintenance;

namespace FirebirdAdmin.Presentation.Wpf.Maintenance;

public sealed class MaintenanceOperationRowViewModel(MaintenanceOperation operation)
{
    public MaintenanceOperation Operation { get; } = operation;
    public string Type => Operation.Type switch
    {
        MaintenanceOperationType.Backup => "Backup",
        MaintenanceOperationType.Restore => "Restore",
        MaintenanceOperationType.Validation => "Validação",
        MaintenanceOperationType.Sweep => "Sweep",
        _ => Operation.Type.ToString()
    };

    public string Status => Operation.Status switch
    {
        MaintenanceOperationStatus.Pending => "Pendente",
        MaintenanceOperationStatus.Running => "Executando",
        MaintenanceOperationStatus.Succeeded => "Concluída",
        MaintenanceOperationStatus.Failed => "Falhou",
        MaintenanceOperationStatus.Cancelled => "Cancelada",
        _ => Operation.Status.ToString()
    };
    public string StartedAt => Operation.StartedAt.ToLocalTime().ToString("g");
    public string FinishedAt => Operation.FinishedAt?.ToLocalTime().ToString("g") ?? "-";
    public string Source => Operation.Source;
    public string Target => Operation.Target ?? "-";
    public string Message => Operation.Message;
}

using FirebirdAdmin.Application.Maintenance;

namespace FirebirdAdmin.Presentation.Wpf.Maintenance;

public sealed class MaintenanceOperationRowViewModel(MaintenanceOperation operation)
{
    public MaintenanceOperation Operation { get; } = operation;
    public string Type => Operation.Type.ToString();
    public string Status => Operation.Status.ToString();
    public string StartedAt => Operation.StartedAt.ToLocalTime().ToString("g");
    public string FinishedAt => Operation.FinishedAt?.ToLocalTime().ToString("g") ?? "-";
    public string Source => Operation.Source;
    public string Target => Operation.Target ?? "-";
    public string Message => Operation.Message;
}

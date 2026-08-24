namespace FirebirdAdmin.Application.Diagnostics;

public interface IAlertStore
{
    Task<Alert> UpsertAsync(DiagnosticResult result, CancellationToken cancellationToken);
    Task<IReadOnlyList<Alert>> ListAsync(AlertStatus? status, DiagnosticSeverity? severity, CancellationToken cancellationToken);
    Task<Alert?> GetByCorrelationKeyAsync(string correlationKey, CancellationToken cancellationToken);
    Task SetStatusAsync(Guid id, AlertStatus status, string? note, CancellationToken cancellationToken);
}

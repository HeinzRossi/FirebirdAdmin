using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FirebirdAdmin.Application.Diagnostics;

namespace FirebirdAdmin.Presentation.Wpf.Diagnostics;

public sealed partial class AlertsCenterViewModel(IAlertStore alertStore) : ObservableObject
{
    [ObservableProperty]
    private string message = "Central de Alertas pronta.";

    [ObservableProperty]
    private string statusFilter = "Active";

    [ObservableProperty]
    private string severityFilter = string.Empty;

    [ObservableProperty]
    private AlertRowViewModel? selectedAlert;

    public ObservableCollection<AlertRowViewModel> Alerts { get; } = [];
    public int ActiveCount => Alerts.Count(alert => alert.Alert.Status is AlertStatus.Active);
    public int CriticalCount => Alerts.Count(alert => alert.Alert.Severity is DiagnosticSeverity.Critical);
    public string SelectedSummary => SelectedAlert?.Message ?? "-";
    public string SelectedEvidence => SelectedAlert?.Evidence ?? "-";
    public string SelectedContext => SelectedAlert is null ? "-" : $"{SelectedAlert.Alert.Target.Type}: {SelectedAlert.Alert.Target.Id}";
    public string SelectedTimeline => SelectedAlert?.Timeline ?? "-";
    public string SelectedRule => SelectedAlert?.RuleId ?? "-";

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var alerts = await alertStore.ListAsync(ParseStatus(StatusFilter), ParseSeverity(SeverityFilter), cancellationToken);
        Alerts.Clear();
        foreach (var alert in alerts)
        {
            Alerts.Add(new AlertRowViewModel(alert));
        }

        Message = $"{Alerts.Count} alerta(s) carregado(s).";
        OnCountsChanged();
    }

    public async Task AcceptDiagnosticResultsAsync(IReadOnlyList<DiagnosticResult> results, CancellationToken cancellationToken = default)
    {
        foreach (var result in results)
        {
            await alertStore.UpsertAsync(result, cancellationToken);
        }

        if (results.Count > 0)
        {
            await LoadAsync(cancellationToken);
        }
    }

    public async Task AcknowledgeAsync(CancellationToken cancellationToken = default)
    {
        await SetSelectedStatusAsync(AlertStatus.Acknowledged, "Reconhecido via Central de Alertas.", cancellationToken);
    }

    public async Task ResolveAsync(CancellationToken cancellationToken = default)
    {
        await SetSelectedStatusAsync(AlertStatus.Resolved, null, cancellationToken);
    }

    public async Task ReopenAsync(CancellationToken cancellationToken = default)
    {
        await SetSelectedStatusAsync(AlertStatus.Active, null, cancellationToken);
    }

    partial void OnSelectedAlertChanged(AlertRowViewModel? value)
    {
        OnPropertyChanged(nameof(SelectedSummary));
        OnPropertyChanged(nameof(SelectedEvidence));
        OnPropertyChanged(nameof(SelectedContext));
        OnPropertyChanged(nameof(SelectedTimeline));
        OnPropertyChanged(nameof(SelectedRule));
    }

    private async Task SetSelectedStatusAsync(AlertStatus status, string? note, CancellationToken cancellationToken)
    {
        if (SelectedAlert is null)
        {
            return;
        }

        await alertStore.SetStatusAsync(SelectedAlert.Id, status, note, cancellationToken);
        await LoadAsync(cancellationToken);
    }

    private void OnCountsChanged()
    {
        OnPropertyChanged(nameof(ActiveCount));
        OnPropertyChanged(nameof(CriticalCount));
    }

    private static AlertStatus? ParseStatus(string value)
    {
        return Enum.TryParse<AlertStatus>(value, ignoreCase: true, out var status) ? status : null;
    }

    private static DiagnosticSeverity? ParseSeverity(string value)
    {
        return Enum.TryParse<DiagnosticSeverity>(value, ignoreCase: true, out var severity) ? severity : null;
    }
}

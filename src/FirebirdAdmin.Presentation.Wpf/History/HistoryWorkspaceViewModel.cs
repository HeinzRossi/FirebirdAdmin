using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FirebirdAdmin.Application.History;
using FirebirdAdmin.Presentation.Wpf.Diagnostics;
using FirebirdAdmin.Presentation.Wpf.Resources;

namespace FirebirdAdmin.Presentation.Wpf.History;

public sealed partial class HistoryWorkspaceViewModel(
    IHistoryQueryService queryService,
    IHistoryExportService exportService) : ObservableObject
{
    [ObservableProperty]
    private string message = "Histórico pronto para pesquisa.";

    [ObservableProperty]
    private string sqlText = string.Empty;

    [ObservableProperty]
    private string userName = string.Empty;

    [ObservableProperty]
    private string dataKind = HistoryDataKind.TraceEvents.ToString();

    [ObservableProperty]
    private int page = 1;

    [ObservableProperty]
    private long totalCount;

    [ObservableProperty]
    private HistoryRowViewModel? selectedRow;

    public ObservableCollection<HistoryRowViewModel> Rows { get; } = [];
    public IReadOnlyList<FilterOption> DataKindOptions { get; } =
    [
        new(AppStrings.HistoryDataKindTraceEvents, HistoryDataKind.TraceEvents.ToString()),
        new(AppStrings.HistoryDataKindMonitoringSnapshots, HistoryDataKind.MonitoringSnapshots.ToString())
    ];

    public string SelectedDetails => SelectedRow?.Details ?? "-";

    public async Task SearchAsync(CancellationToken cancellationToken = default)
    {
        Rows.Clear();
        var query = CreateQuery();

        if (query.Kind is HistoryDataKind.MonitoringSnapshots)
        {
            var pageResult = await queryService.QueryMonitoringSnapshotsAsync(query, cancellationToken);
            foreach (var item in pageResult.Items)
            {
                Rows.Add(new HistoryRowViewModel(item));
            }

            TotalCount = pageResult.TotalCount;
        }
        else
        {
            var pageResult = await queryService.QueryTraceEventsAsync(query, cancellationToken);
            foreach (var item in pageResult.Items)
            {
                Rows.Add(new HistoryRowViewModel(item));
            }

            TotalCount = pageResult.TotalCount;
        }

        Message = $"{Rows.Count} item(ns) carregado(s) de {TotalCount}.";
    }

    public async Task ExportCsvAsync(CancellationToken cancellationToken = default)
    {
        await ExportAsync(ExportFormat.Csv, cancellationToken);
    }

    public async Task ExportJsonAsync(CancellationToken cancellationToken = default)
    {
        await ExportAsync(ExportFormat.Json, cancellationToken);
    }

    partial void OnSelectedRowChanged(HistoryRowViewModel? value)
    {
        OnPropertyChanged(nameof(SelectedDetails));
    }

    private async Task ExportAsync(ExportFormat format, CancellationToken cancellationToken)
    {
        var result = await exportService.ExportAsync(new ExportRequest(CreateQuery(), format), cancellationToken);
        Message = $"Exportado: {result.RowCount} linha(s) em {result.OutputPath}";
    }

    private HistoryQuery CreateQuery()
    {
        return new HistoryQuery(
            Kind: string.Equals(DataKind, HistoryDataKind.MonitoringSnapshots.ToString(), StringComparison.OrdinalIgnoreCase)
                ? HistoryDataKind.MonitoringSnapshots
                : HistoryDataKind.TraceEvents,
            SqlText: string.IsNullOrWhiteSpace(SqlText) ? null : SqlText,
            UserName: string.IsNullOrWhiteSpace(UserName) ? null : UserName,
            Page: Math.Max(Page, 1),
            PageSize: 100);
    }
}

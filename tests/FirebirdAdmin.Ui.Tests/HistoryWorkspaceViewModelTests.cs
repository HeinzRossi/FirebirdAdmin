using FirebirdAdmin.Application.History;
using FirebirdAdmin.Application.Profiler;
using FirebirdAdmin.Presentation.Wpf.History;
using FluentAssertions;

namespace FirebirdAdmin.Ui.Tests;

public sealed class HistoryWorkspaceViewModelTests
{
    [Fact]
    public void Constructor_ShouldStartEmpty()
    {
        var viewModel = new HistoryWorkspaceViewModel(new FakeHistoryQueryService(), new FakeHistoryExportService());

        viewModel.Rows.Should().BeEmpty();
        viewModel.Message.Should().Contain("Histórico");
        viewModel.DataKindOptions.Should().Contain(option => option.Label == "Eventos Trace" && option.Value == HistoryDataKind.TraceEvents.ToString());
        viewModel.DataKindOptions.Should().Contain(option => option.Label == "Snapshots de monitoramento" && option.Value == HistoryDataKind.MonitoringSnapshots.ToString());
    }

    [Fact]
    public async Task SearchAsync_ShouldLoadTraceRows()
    {
        var viewModel = new HistoryWorkspaceViewModel(new FakeHistoryQueryService(), new FakeHistoryExportService());

        await viewModel.SearchAsync();

        viewModel.Rows.Should().ContainSingle();
        viewModel.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task ExportCsvAsync_ShouldReportSuccess()
    {
        var viewModel = new HistoryWorkspaceViewModel(new FakeHistoryQueryService(), new FakeHistoryExportService());

        await viewModel.ExportCsvAsync();

        viewModel.Message.Should().Contain("Exportado");
    }

    [Fact]
    public async Task SearchAsync_ShouldUseMonitoringKind_WhenSelectedFromCombo()
    {
        var queryService = new FakeHistoryQueryService();
        var viewModel = new HistoryWorkspaceViewModel(queryService, new FakeHistoryExportService())
        {
            DataKind = HistoryDataKind.MonitoringSnapshots.ToString()
        };

        await viewModel.SearchAsync();

        queryService.LastKind.Should().Be(HistoryDataKind.MonitoringSnapshots);
    }

    private sealed class FakeHistoryQueryService : IHistoryQueryService
    {
        public HistoryDataKind? LastKind { get; private set; }

        public Task<HistoryPage<TraceEventHistoryItem>> QueryTraceEventsAsync(HistoryQuery query, CancellationToken cancellationToken)
        {
            LastKind = query.Kind;
            TraceEventHistoryItem[] items =
            [
                new(1, null, 1, DateTimeOffset.UtcNow, TraceEventType.StatementFinished, TimeSpan.FromMilliseconds(1), "SYSDBA", 1, 2, "select 1", null, "raw")
            ];
            return Task.FromResult(new HistoryPage<TraceEventHistoryItem>(items, 1, 100, 1));
        }

        public Task<HistoryPage<MonitoringSnapshotHistoryItem>> QueryMonitoringSnapshotsAsync(HistoryQuery query, CancellationToken cancellationToken)
        {
            LastKind = query.Kind;
            return Task.FromResult(new HistoryPage<MonitoringSnapshotHistoryItem>([], 1, 100, 0));
        }
    }

    private sealed class FakeHistoryExportService : IHistoryExportService
    {
        public Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new ExportResult("history.csv", 1));
        }
    }
}

using FirebirdAdmin.Application.Dashboard;
using FirebirdAdmin.Application.Monitoring;
using FirebirdAdmin.Presentation.Wpf.Dashboard;
using FluentAssertions;

namespace FirebirdAdmin.Ui.Tests;

public sealed class DashboardViewModelTests
{
    [Fact]
    public void Constructor_ShouldStartDisconnected()
    {
        var viewModel = new DashboardViewModel(new DashboardProjectionService());

        viewModel.Health.Should().Be(DatabaseHealthStatus.Disconnected);
        viewModel.HealthText.Should().Be("Disconnected");
        viewModel.Metrics.Should().Contain(metric => metric.Key == "attachments" && metric.Value == "0");
    }

    [Fact]
    public void ApplySnapshot_ShouldUpdateMetricsAndActivity()
    {
        var viewModel = new DashboardViewModel(new DashboardProjectionService());

        viewModel.ApplySnapshot(CreateSnapshot(DateTimeOffset.UtcNow, activeStatements: 2));

        viewModel.Health.Should().Be(DatabaseHealthStatus.Healthy);
        viewModel.HealthText.Should().Be("Ready");
        viewModel.Metrics.Should().Contain(metric => metric.Key == "statements" && metric.Value == "2");
        viewModel.Activity.Should().ContainSingle(point => point.ActiveStatements == 2);
    }

    [Fact]
    public void ApplySnapshot_ShouldLimitActivityWindow()
    {
        var viewModel = new DashboardViewModel(new DashboardProjectionService());
        var start = DateTimeOffset.UtcNow;

        for (var index = 0; index < 65; index++)
        {
            viewModel.ApplySnapshot(CreateSnapshot(start.AddSeconds(index), activeStatements: index));
        }

        viewModel.Activity.Should().HaveCount(DashboardViewModel.ActivityWindowSize);
        viewModel.Activity.First().ActiveStatements.Should().Be(5);
        viewModel.Activity.Last().ActiveStatements.Should().Be(64);
    }

    [Fact]
    public void SetError_ShouldSetCriticalHealth()
    {
        var viewModel = new DashboardViewModel(new DashboardProjectionService());

        viewModel.SetError("Falha no polling");

        viewModel.Health.Should().Be(DatabaseHealthStatus.Critical);
        viewModel.HealthMessage.Should().Be("Falha no polling");
    }

    private static MonitoringSnapshot CreateSnapshot(DateTimeOffset capturedAt, int activeStatements)
    {
        return new MonitoringSnapshot(
            Guid.NewGuid(),
            capturedAt,
            [new AttachmentSnapshot(1, "SYSDBA", "127.0.0.1", "isql", capturedAt, "1")],
            [new TransactionSnapshot(10, 1, "1", capturedAt, 5, 6, 7, 8)],
            Enumerable.Range(0, activeStatements)
                .Select(index => new StatementSnapshot(20 + index, 1, 10, "1", capturedAt, "select 1 from rdb$database"))
                .ToArray());
    }
}

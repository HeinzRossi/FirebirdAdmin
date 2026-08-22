using FirebirdAdmin.Application.Dashboard;
using FirebirdAdmin.Application.Monitoring;
using FluentAssertions;

namespace FirebirdAdmin.Application.Tests;

public sealed class DashboardProjectionServiceTests
{
    [Fact]
    public void CreateDisconnected_ShouldReturnDisconnectedDashboard()
    {
        var service = new DashboardProjectionService();

        var snapshot = service.CreateDisconnected();

        snapshot.Health.Should().Be(DatabaseHealthStatus.Disconnected);
        snapshot.LastUpdatedAt.Should().BeNull();
        snapshot.Metrics.Should().Contain(metric => metric.Key == "attachments" && metric.Value == "0");
    }

    [Fact]
    public void Project_ShouldReturnHealthyMetricsForRecentSnapshot()
    {
        var service = new DashboardProjectionService();
        var capturedAt = DateTimeOffset.UtcNow;

        var dashboard = service.Project(CreateSnapshot(capturedAt), capturedAt);

        dashboard.Health.Should().Be(DatabaseHealthStatus.Healthy);
        dashboard.Metrics.Should().Contain(metric => metric.Key == "attachments" && metric.Value == "1");
        dashboard.Metrics.Should().Contain(metric => metric.Key == "transactions" && metric.Value == "1");
        dashboard.Metrics.Should().Contain(metric => metric.Key == "statements" && metric.Value == "1");
        dashboard.Activity.Should().ContainSingle(point => point.ActiveStatements == 1);
    }

    [Fact]
    public void Project_ShouldHandleEmptySnapshot()
    {
        var service = new DashboardProjectionService();
        var capturedAt = DateTimeOffset.UtcNow;
        var snapshot = new MonitoringSnapshot(Guid.NewGuid(), capturedAt, [], [], []);

        var dashboard = service.Project(snapshot, capturedAt);

        dashboard.Health.Should().Be(DatabaseHealthStatus.Healthy);
        dashboard.Metrics.Should().Contain(metric => metric.Key == "attachments" && metric.Value == "0");
        dashboard.Metrics.Should().Contain(metric => metric.Key == "transactions" && metric.Value == "0");
        dashboard.Metrics.Should().Contain(metric => metric.Key == "statements" && metric.Value == "0");
    }

    [Fact]
    public void Project_ShouldReturnStaleForOldSnapshot()
    {
        var service = new DashboardProjectionService();
        var now = DateTimeOffset.UtcNow;

        var dashboard = service.Project(CreateSnapshot(now.AddSeconds(-30)), now);

        dashboard.Health.Should().Be(DatabaseHealthStatus.Stale);
    }

    [Fact]
    public void Project_ShouldReturnWarningWhenTransactionTimestampMissing()
    {
        var service = new DashboardProjectionService();
        var now = DateTimeOffset.UtcNow;
        var snapshot = CreateSnapshot(now) with
        {
            Transactions = [new TransactionSnapshot(10, 1, "1", null, 5, 6, 7, 8)]
        };

        var dashboard = service.Project(snapshot, now);

        dashboard.Health.Should().Be(DatabaseHealthStatus.Warning);
    }

    [Fact]
    public void ProjectError_ShouldReturnCritical()
    {
        var service = new DashboardProjectionService();

        var dashboard = service.ProjectError("Erro MON$", DateTimeOffset.UtcNow);

        dashboard.Health.Should().Be(DatabaseHealthStatus.Critical);
        dashboard.HealthMessage.Should().Be("Erro MON$");
    }

    private static MonitoringSnapshot CreateSnapshot(DateTimeOffset capturedAt)
    {
        return new MonitoringSnapshot(
            Guid.NewGuid(),
            capturedAt,
            [new AttachmentSnapshot(1, "SYSDBA", "127.0.0.1", "isql", capturedAt, "1")],
            [new TransactionSnapshot(10, 1, "1", capturedAt, 5, 6, 7, 8)],
            [new StatementSnapshot(20, 1, 10, "1", capturedAt, "select 1 from rdb$database")]);
    }
}

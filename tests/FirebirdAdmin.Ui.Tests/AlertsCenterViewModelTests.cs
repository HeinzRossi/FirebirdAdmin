using FirebirdAdmin.Application.Diagnostics;
using FirebirdAdmin.Presentation.Wpf.Diagnostics;
using FluentAssertions;

namespace FirebirdAdmin.Ui.Tests;

public sealed class AlertsCenterViewModelTests
{
    [Fact]
    public void Constructor_ShouldStartEmpty()
    {
        var viewModel = new AlertsCenterViewModel(new FakeAlertStore());

        viewModel.Alerts.Should().BeEmpty();
        viewModel.Message.Should().Contain("Central");
    }

    [Fact]
    public async Task AcceptDiagnosticResultsAsync_ShouldLoadAlerts()
    {
        var viewModel = new AlertsCenterViewModel(new FakeAlertStore());

        await viewModel.AcceptDiagnosticResultsAsync([CreateResult()]);

        viewModel.Alerts.Should().ContainSingle();
        viewModel.ActiveCount.Should().Be(1);
    }

    [Fact]
    public async Task Actions_ShouldUpdateStatus()
    {
        var viewModel = new AlertsCenterViewModel(new FakeAlertStore());
        await viewModel.AcceptDiagnosticResultsAsync([CreateResult()]);
        viewModel.SelectedAlert = viewModel.Alerts.Single();

        await viewModel.AcknowledgeAsync();

        viewModel.StatusFilter = string.Empty;
        await viewModel.LoadAsync();
        viewModel.Alerts.Single().Alert.Status.Should().Be(AlertStatus.Acknowledged);

        viewModel.SelectedAlert = viewModel.Alerts.Single();
        await viewModel.ResolveAsync();

        viewModel.StatusFilter = string.Empty;
        await viewModel.LoadAsync();
        viewModel.Alerts.Single().Alert.Status.Should().Be(AlertStatus.Resolved);
    }

    private static DiagnosticResult CreateResult()
    {
        return new DiagnosticResult(
            "RULE",
            DiagnosticSeverity.Low,
            "msg",
            new DiagnosticTarget("Target", "1"),
            DateTimeOffset.UtcNow,
            Guid.NewGuid(),
            Guid.NewGuid(),
            [new DiagnosticEvidence("Value", 1)]);
    }

    private sealed class FakeAlertStore : IAlertStore
    {
        private readonly AlertCorrelator correlator = new();
        private readonly List<Alert> alerts = [];

        public Task<Alert> UpsertAsync(DiagnosticResult result, CancellationToken cancellationToken)
        {
            var key = AlertCorrelator.BuildCorrelationKey(result);
            var existing = alerts.SingleOrDefault(alert => alert.CorrelationKey == key);
            var alert = correlator.Correlate(result, existing);
            if (existing is not null)
            {
                alerts.Remove(existing);
            }

            alerts.Add(alert);
            return Task.FromResult(alert);
        }

        public Task<IReadOnlyList<Alert>> ListAsync(AlertStatus? status, DiagnosticSeverity? severity, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<Alert>>(alerts.Where(alert =>
                (status is null || alert.Status == status) &&
                (severity is null || alert.Severity == severity)).ToArray());
        }

        public Task<Alert?> GetByCorrelationKeyAsync(string correlationKey, CancellationToken cancellationToken)
        {
            return Task.FromResult(alerts.SingleOrDefault(alert => alert.CorrelationKey == correlationKey));
        }

        public Task SetStatusAsync(Guid id, AlertStatus status, string? note, CancellationToken cancellationToken)
        {
            var alert = alerts.Single(alert => alert.Id == id);
            alerts.Remove(alert);
            alerts.Add(alert with { Status = status });
            return Task.CompletedTask;
        }
    }
}

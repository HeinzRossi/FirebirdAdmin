using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FirebirdAdmin.Application.Dashboard;
using FirebirdAdmin.Application.Monitoring;

namespace FirebirdAdmin.Presentation.Wpf.Dashboard;

public sealed partial class DashboardViewModel : ObservableObject
{
    public const int ActivityWindowSize = 60;

    private readonly IDashboardProjectionService projectionService;
    private readonly Dictionary<string, DashboardMetricViewModel> metricsByKey = [];

    [ObservableProperty]
    private DatabaseHealthStatus health;

    [ObservableProperty]
    private string healthText = string.Empty;

    [ObservableProperty]
    private string healthMessage = string.Empty;

    [ObservableProperty]
    private DateTimeOffset? lastUpdatedAt;

    [ObservableProperty]
    private string lastUpdatedText = "-";

    public DashboardViewModel(IDashboardProjectionService projectionService)
    {
        this.projectionService = projectionService;
        ApplyDashboard(projectionService.CreateDisconnected(), resetActivity: true);
    }

    public ObservableCollection<DashboardMetricViewModel> Metrics { get; } = [];
    public ObservableCollection<ActivityPoint> Activity { get; } = [];

    public event EventHandler? ActivityChanged;

    public void ApplySnapshot(MonitoringSnapshot snapshot)
    {
        ApplyDashboard(projectionService.Project(snapshot, DateTimeOffset.UtcNow), resetActivity: false);
    }

    public void SetError(string message)
    {
        ApplyDashboard(projectionService.ProjectError(message, DateTimeOffset.UtcNow), resetActivity: false);
    }

    public double[] GetActivityValues()
    {
        return Activity.Select(point => point.ActiveStatements).ToArray();
    }

    private void ApplyDashboard(DashboardSnapshot snapshot, bool resetActivity)
    {
        Health = snapshot.Health;
        HealthText = ToHealthText(snapshot.Health);
        HealthMessage = snapshot.HealthMessage;
        LastUpdatedAt = snapshot.LastUpdatedAt;
        LastUpdatedText = snapshot.LastUpdatedAt?.ToLocalTime().ToString("HH:mm:ss") ?? "-";

        ApplyMetrics(snapshot.Metrics);
        ApplyActivity(snapshot.Activity, resetActivity);
    }

    private void ApplyMetrics(IReadOnlyList<DashboardMetric> metrics)
    {
        var incomingKeys = metrics.Select(metric => metric.Key).ToHashSet(StringComparer.Ordinal);

        foreach (var removedKey in metricsByKey.Keys.Except(incomingKeys, StringComparer.Ordinal).ToArray())
        {
            var metric = metricsByKey[removedKey];
            metricsByKey.Remove(removedKey);
            Metrics.Remove(metric);
        }

        foreach (var metric in metrics)
        {
            if (metricsByKey.TryGetValue(metric.Key, out var existing))
            {
                existing.Apply(metric);
                continue;
            }

            var viewModel = new DashboardMetricViewModel(metric);
            metricsByKey.Add(metric.Key, viewModel);
            Metrics.Add(viewModel);
        }
    }

    private void ApplyActivity(IReadOnlyList<ActivityPoint> points, bool resetActivity)
    {
        if (resetActivity)
        {
            Activity.Clear();
        }

        foreach (var point in points)
        {
            Activity.Add(point);
        }

        while (Activity.Count > ActivityWindowSize)
        {
            Activity.RemoveAt(0);
        }

        ActivityChanged?.Invoke(this, EventArgs.Empty);
    }

    private static string ToHealthText(DatabaseHealthStatus health)
    {
        return health switch
        {
            DatabaseHealthStatus.Disconnected => "Disconnected",
            DatabaseHealthStatus.Healthy => "Ready",
            DatabaseHealthStatus.Warning => "Warning",
            DatabaseHealthStatus.Critical => "Critical",
            DatabaseHealthStatus.Stale => "Stale",
            _ => health.ToString()
        };
    }
}

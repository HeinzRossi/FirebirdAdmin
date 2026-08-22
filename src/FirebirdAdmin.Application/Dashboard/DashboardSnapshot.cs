namespace FirebirdAdmin.Application.Dashboard;

public sealed record DashboardSnapshot(
    DatabaseHealthStatus Health,
    string HealthMessage,
    DateTimeOffset? LastUpdatedAt,
    IReadOnlyList<DashboardMetric> Metrics,
    IReadOnlyList<ActivityPoint> Activity);

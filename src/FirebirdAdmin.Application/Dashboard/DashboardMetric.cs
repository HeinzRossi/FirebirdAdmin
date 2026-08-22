namespace FirebirdAdmin.Application.Dashboard;

public sealed record DashboardMetric(
    string Key,
    string Label,
    string Value,
    string? Detail = null);

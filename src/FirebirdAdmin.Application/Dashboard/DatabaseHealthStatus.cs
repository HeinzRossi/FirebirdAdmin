namespace FirebirdAdmin.Application.Dashboard;

public enum DatabaseHealthStatus
{
    Disconnected,
    Healthy,
    Warning,
    Critical,
    Stale
}

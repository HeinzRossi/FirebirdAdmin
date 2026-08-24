using FirebirdAdmin.Application.Monitoring;

namespace FirebirdAdmin.Application.Dashboard;

public interface IDashboardProjectionService
{
    DashboardSnapshot CreateDisconnected();
    DashboardSnapshot Project(MonitoringSnapshot snapshot, DateTimeOffset now);
    DashboardSnapshot ProjectError(string message, DateTimeOffset now);
}

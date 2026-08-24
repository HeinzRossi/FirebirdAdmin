namespace FirebirdAdmin.Application.Monitoring;

public sealed record MonitoringSessionStatus(
    PollingState State,
    string Message,
    DateTimeOffset UpdatedAt);

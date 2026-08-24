using FirebirdAdmin.Application.Connections;

namespace FirebirdAdmin.Application.Monitoring;

public sealed record MonitoringSession(
    Guid Id,
    ConnectionContext Connection,
    PollingOptions Options,
    DateTimeOffset StartedAt);

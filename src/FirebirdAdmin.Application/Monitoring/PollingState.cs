namespace FirebirdAdmin.Application.Monitoring;

public enum PollingState
{
    Disconnected,
    Connecting,
    Connected,
    Reconnecting,
    Stopped,
    Failed
}

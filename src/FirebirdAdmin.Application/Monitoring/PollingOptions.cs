namespace FirebirdAdmin.Application.Monitoring;

public sealed record PollingOptions(MonitoringPreset Preset, TimeSpan MinInterval, TimeSpan MaxInterval)
{
    public static PollingOptions Aggressive { get; } = new(MonitoringPreset.Aggressive, TimeSpan.FromMilliseconds(250), TimeSpan.FromSeconds(2));
    public static PollingOptions Normal { get; } = new(MonitoringPreset.Normal, TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(5));
    public static PollingOptions Conservative { get; } = new(MonitoringPreset.Conservative, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10));

    public static PollingOptions CreateCustom(TimeSpan minInterval, TimeSpan maxInterval)
    {
        if (minInterval <= TimeSpan.Zero || maxInterval <= TimeSpan.Zero || minInterval > maxInterval)
        {
            throw new ArgumentOutOfRangeException(nameof(minInterval), "Polling intervals must be positive and min must be <= max.");
        }

        return new PollingOptions(MonitoringPreset.Custom, minInterval, maxInterval);
    }
}

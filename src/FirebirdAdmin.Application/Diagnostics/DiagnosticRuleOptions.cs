namespace FirebirdAdmin.Application.Diagnostics;

public sealed record DiagnosticRuleOptions(
    DiagnosticPreset Preset = DiagnosticPreset.Normal,
    TimeSpan? LongTransactionThreshold = null,
    int? AttachmentWarningThreshold = null,
    TimeSpan? SlowStatementThreshold = null,
    TimeSpan? StaleSnapshotThreshold = null)
{
    public static DiagnosticRuleOptions Normal { get; } = new();

    public TimeSpan EffectiveLongTransactionThreshold => LongTransactionThreshold ?? Preset switch
    {
        DiagnosticPreset.Aggressive => TimeSpan.FromMinutes(5),
        DiagnosticPreset.Conservative => TimeSpan.FromMinutes(30),
        _ => TimeSpan.FromMinutes(15)
    };

    public int EffectiveAttachmentWarningThreshold => AttachmentWarningThreshold ?? Preset switch
    {
        DiagnosticPreset.Aggressive => 25,
        DiagnosticPreset.Conservative => 150,
        _ => 75
    };

    public TimeSpan EffectiveSlowStatementThreshold => SlowStatementThreshold ?? Preset switch
    {
        DiagnosticPreset.Aggressive => TimeSpan.FromMilliseconds(500),
        DiagnosticPreset.Conservative => TimeSpan.FromSeconds(10),
        _ => TimeSpan.FromSeconds(2)
    };

    public TimeSpan EffectiveStaleSnapshotThreshold => StaleSnapshotThreshold ?? TimeSpan.FromSeconds(10);
}

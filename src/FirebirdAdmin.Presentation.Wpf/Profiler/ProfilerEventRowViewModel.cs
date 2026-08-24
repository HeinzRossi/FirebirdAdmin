using CommunityToolkit.Mvvm.ComponentModel;
using FirebirdAdmin.Application.Profiler;

namespace FirebirdAdmin.Presentation.Wpf.Profiler;

public sealed partial class ProfilerEventRowViewModel : ObservableObject
{
    public ProfilerEventRowViewModel(ProfilerEvent profilerEvent)
    {
        Event = profilerEvent;
    }

    public ProfilerEvent Event { get; }
    public long Sequence => Event.Sequence;
    public string Timestamp => Event.Timestamp.ToLocalTime().ToString("HH:mm:ss.fff");
    public string Type => Event.Type.ToString();
    public string Duration => Event.Duration?.TotalMilliseconds.ToString("0.###") ?? "-";
    public string? UserName => Event.UserName;
    public long? AttachmentId => Event.AttachmentId;
    public long? TransactionId => Event.TransactionId;
    public string SqlPreview => string.IsNullOrWhiteSpace(Event.Sql) ? Event.RawTrace : Event.Sql;
    public string Reads => Event.Metrics.Reads?.ToString() ?? "-";
    public string Writes => Event.Metrics.Writes?.ToString() ?? "-";
    public string Fetches => Event.Metrics.Fetches?.ToString() ?? "-";
    public string Plan => Event.Plan ?? "-";
    public string RawTrace => Event.RawTrace;
}

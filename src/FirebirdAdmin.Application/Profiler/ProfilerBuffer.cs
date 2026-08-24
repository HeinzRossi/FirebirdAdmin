namespace FirebirdAdmin.Application.Profiler;

public sealed class ProfilerBuffer(int maxEvents = 5000, TimeSpan? maxAge = null) : IProfilerBuffer
{
    public const int DefaultMaxEvents = 5000;
    public static readonly TimeSpan DefaultMaxAge = TimeSpan.FromMinutes(10);

    private readonly List<ProfilerEvent> events = [];

    public IReadOnlyList<ProfilerEvent> Events => events;

    public void Add(ProfilerEvent profilerEvent)
    {
        events.Add(profilerEvent);
        Trim(profilerEvent.Timestamp);
    }

    public void Clear()
    {
        events.Clear();
    }

    public IReadOnlyList<ProfilerEvent> ApplyFilter(ProfilerFilter filter)
    {
        return events.Where(filter.Matches).ToArray();
    }

    private void Trim(DateTimeOffset now)
    {
        while (events.Count > maxEvents)
        {
            events.RemoveAt(0);
        }

        var cutoff = now - (maxAge ?? DefaultMaxAge);
        while (events.Count > 0 && events[0].Timestamp < cutoff)
        {
            events.RemoveAt(0);
        }
    }
}

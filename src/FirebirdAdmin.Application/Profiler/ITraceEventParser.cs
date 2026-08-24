namespace FirebirdAdmin.Application.Profiler;

public interface ITraceEventParser
{
    IReadOnlyList<ProfilerEvent> ParseBlock(string block, long startingSequence, DateTimeOffset timestamp);
}

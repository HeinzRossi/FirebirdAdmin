using FirebirdAdmin.Application.Profiler;
using FluentAssertions;

namespace FirebirdAdmin.Application.Tests;

public sealed class ProfilerParserTests
{
    [Fact]
    public void ParseBlock_ShouldNormalizeStatementFinish()
    {
        var parser = new FirebirdTraceEventParser();
        const string block = """
                             statement finished
                             user: SYSDBA
                             attachment: 12
                             transaction: 34
                             duration: 15.5 ms
                             sql: select 1 from rdb$database
                             plan natural
                             reads: 3 writes: 1 fetches: 2 marks: 4
                             """;

        var events = parser.ParseBlock(block, 10, DateTimeOffset.UtcNow);

        events.Should().ContainSingle();
        var profilerEvent = events[0];
        profilerEvent.Sequence.Should().Be(10);
        profilerEvent.Type.Should().Be(TraceEventType.StatementFinished);
        profilerEvent.UserName.Should().Be("SYSDBA");
        profilerEvent.AttachmentId.Should().Be(12);
        profilerEvent.TransactionId.Should().Be(34);
        profilerEvent.Duration.Should().Be(TimeSpan.FromMilliseconds(15.5));
        profilerEvent.Sql.Should().Be("select 1 from rdb$database");
        profilerEvent.Plan.Should().Be("plan natural");
        profilerEvent.Metrics.Reads.Should().Be(3);
        profilerEvent.Metrics.Writes.Should().Be(1);
        profilerEvent.Metrics.Fetches.Should().Be(2);
        profilerEvent.Metrics.Marks.Should().Be(4);
    }

    [Fact]
    public void ParseBlock_ShouldPreserveUnparsedLine()
    {
        var parser = new FirebirdTraceEventParser();

        var events = parser.ParseBlock("linha truncada sem formato", 1, DateTimeOffset.UtcNow);

        events.Should().ContainSingle();
        events[0].Type.Should().Be(TraceEventType.Unparsed);
        events[0].RawTrace.Should().Contain("linha truncada");
    }
}

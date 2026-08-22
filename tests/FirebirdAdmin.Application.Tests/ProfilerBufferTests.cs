using FirebirdAdmin.Application.Profiler;
using FluentAssertions;

namespace FirebirdAdmin.Application.Tests;

public sealed class ProfilerBufferTests
{
    [Fact]
    public void Add_ShouldLimitByCountAndPreserveOrder()
    {
        var buffer = new ProfilerBuffer(maxEvents: 3);
        var now = DateTimeOffset.UtcNow;

        for (var index = 1; index <= 5; index++)
        {
            buffer.Add(CreateEvent(index, now.AddSeconds(index), sql: $"select {index}"));
        }

        buffer.Events.Select(profilerEvent => profilerEvent.Sequence).Should().Equal(3, 4, 5);
    }

    [Fact]
    public void ApplyFilter_ShouldFilterBySqlUserDurationAttachmentAndTransaction()
    {
        var buffer = new ProfilerBuffer();
        var now = DateTimeOffset.UtcNow;
        buffer.Add(CreateEvent(1, now, "select * from customers", "SYSDBA", TimeSpan.FromMilliseconds(50), 10, 20));
        buffer.Add(CreateEvent(2, now, "update orders set id = id", "APP", TimeSpan.FromMilliseconds(2), 11, 21));

        var results = buffer.ApplyFilter(new ProfilerFilter(
            SqlText: "customers",
            UserName: "SYSDBA",
            MinimumDuration: TimeSpan.FromMilliseconds(10),
            AttachmentId: 10,
            TransactionId: 20));

        results.Should().ContainSingle();
        results[0].Sequence.Should().Be(1);
    }

    private static ProfilerEvent CreateEvent(
        long sequence,
        DateTimeOffset timestamp,
        string sql,
        string userName = "SYSDBA",
        TimeSpan? duration = null,
        long? attachmentId = null,
        long? transactionId = null)
    {
        return new ProfilerEvent(
            sequence,
            timestamp,
            TraceEventType.StatementFinished,
            duration,
            userName,
            attachmentId,
            transactionId,
            sql,
            new ProfilerMetrics(),
            null,
            sql);
    }
}

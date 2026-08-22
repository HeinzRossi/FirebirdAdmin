using FirebirdAdmin.Application.Connections;
using FirebirdAdmin.Application.Profiler;
using FirebirdAdmin.Infrastructure.Profiler;
using FluentAssertions;

namespace FirebirdAdmin.Infrastructure.Tests;

public sealed class FbTraceManagerProfilerSessionServiceTests
{
    [Fact]
    public async Task StartAsync_ShouldUseFetchFileAndPublishParsedEvents()
    {
        var runner = new FakeTraceProcessRunner();
        var service = new FbTraceManagerProfilerSessionService(
            new TraceConfigurationBuilder(),
            new FirebirdTraceEventParser(),
            runner);

        using var secret = CredentialSecret.FromPlainText("masterkey");

        await service.StartAsync(CreateOptions(), secret, CancellationToken.None);
        var profilerEvent = await ReadOneAsync(service);
        await service.StopAsync(CancellationToken.None);

        runner.Request.Should().NotBeNull();
        runner.Request!.Arguments.Should().Contain("-fetch");
        runner.Request.Arguments.Should().NotContain("-password");
        runner.Request.Arguments.Should().NotContain("masterkey");
        runner.FetchPath.Should().NotBeNull();
        File.Exists(runner.FetchPath!).Should().BeFalse();
        profilerEvent.Type.Should().Be(TraceEventType.StatementFinished);
        profilerEvent.Sql.Should().Be("select 1 from rdb$database");
    }

    private static async Task<ProfilerEvent> ReadOneAsync(IProfilerSessionService service)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await foreach (var profilerEvent in service.ReadAllAsync(timeout.Token))
        {
            return profilerEvent;
        }

        throw new InvalidOperationException("Nenhum evento recebido.");
    }

    private static ProfilerOptions CreateOptions()
    {
        var toolset = new EffectiveToolset(
        [
            new ToolsetCandidate(FirebirdToolKind.TraceManager, "fake-fbtracemgr.exe", null, IsAvailable: true)
        ]);

        var context = new ConnectionContext(
            Guid.NewGuid(),
            "Local",
            "localhost",
            3050,
            "employee.fdb",
            "SYSDBA",
            FirebirdServerVersion.Parse("5.0.0"),
            new FirebirdCapabilities(true, true, true, true, true, "ok"),
            toolset,
            DateTimeOffset.UtcNow);

        return new ProfilerOptions(context, "test");
    }

    private sealed class FakeTraceProcessRunner : ITraceProcessRunner
    {
        public TraceProcessRequest? Request { get; private set; }
        public string? FetchPath { get; private set; }

        public async Task<int> RunAsync(
            TraceProcessRequest request,
            Func<string, CancellationToken, Task> onOutputLine,
            Func<string, CancellationToken, Task> onErrorLine,
            CancellationToken cancellationToken)
        {
            Request = request;
            var arguments = request.Arguments.ToArray();
            var fetchIndex = Array.IndexOf(arguments, "-fetch");
            FetchPath = fetchIndex >= 0 ? arguments[fetchIndex + 1] : null;

            await onOutputLine("statement finished", cancellationToken);
            await onOutputLine("user: SYSDBA", cancellationToken);
            await onOutputLine("attachment: 1", cancellationToken);
            await onOutputLine("transaction: 2", cancellationToken);
            await onOutputLine("duration: 1 ms", cancellationToken);
            await onOutputLine("sql: select 1 from rdb$database", cancellationToken);
            await onOutputLine(string.Empty, cancellationToken);
            return 0;
        }
    }
}

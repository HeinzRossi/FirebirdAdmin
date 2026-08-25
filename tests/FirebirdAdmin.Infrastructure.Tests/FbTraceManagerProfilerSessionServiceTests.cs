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
        runner.FetchFileExistedDuringRun.Should().BeTrue();
        runner.FetchFileContentDuringRun.Should().Be($"masterkey{Environment.NewLine}");
        File.Exists(runner.FetchPath!).Should().BeFalse();
        profilerEvent.Type.Should().Be(TraceEventType.StatementFinished);
        profilerEvent.Sql.Should().Be("select 1 from rdb$database");
    }

    [Fact]
    public async Task StartAsync_ShouldNotUseFetchFile_WhenPasswordIsNull()
    {
        var runner = new FakeTraceProcessRunner();
        var service = new FbTraceManagerProfilerSessionService(
            new TraceConfigurationBuilder(),
            new FirebirdTraceEventParser(),
            runner);

        await service.StartAsync(CreateOptions(), password: null, CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        runner.Request.Should().NotBeNull();
        runner.Request!.Arguments.Should().NotContain("-fetch");
        runner.FetchPath.Should().BeNull();
    }

    [Fact]
    public async Task StartAsync_ShouldPublishTechnicalEventAndFail_WhenProcessReturnsNonZero()
    {
        var runner = new FakeTraceProcessRunner(exitCode: 2, emitStatement: false);
        var service = new FbTraceManagerProfilerSessionService(
            new TraceConfigurationBuilder(),
            new FirebirdTraceEventParser(),
            runner);

        using var secret = CredentialSecret.FromPlainText("masterkey");

        await service.StartAsync(CreateOptions(), secret, CancellationToken.None);
        var profilerEvent = await ReadOneAsync(service);

        profilerEvent.Type.Should().Be(TraceEventType.Technical);
        profilerEvent.RawTrace.Should().Contain("fbtracemgr finalizou com código 2");
        service.State.Should().Be(ProfilerState.Failed);
    }

    [Fact]
    public async Task StartAsync_ShouldChooseModernDialect_WhenTraceManagerVersionIsModernEvenWithFirebird25Server()
    {
        var runner = new FakeTraceProcessRunner();
        var service = new FbTraceManagerProfilerSessionService(
            new TraceConfigurationBuilder(),
            new FirebirdTraceEventParser(),
            runner);

        using var secret = CredentialSecret.FromPlainText("masterkey");

        await service.StartAsync(CreateOptions(serverVersion: "2.5.9", traceManagerVersion: "Firebird Trace Manager version 5.0.0"), secret, CancellationToken.None);
        var profilerEvent = await ReadOneAsync(service);
        await service.StopAsync(CancellationToken.None);

        runner.ConfigContentDuringRun.Should().Contain("database = employee.fdb");
        runner.ConfigContentDuringRun.Should().Contain("enabled = true");
        runner.ConfigContentDuringRun.Should().NotContain("</database>");
        profilerEvent.Type.Should().Be(TraceEventType.StatementFinished);
    }

    [Fact]
    public async Task StartAsync_ShouldRetryWithAlternateDialect_WhenStartupReportsTraceConfigurationParseError()
    {
        var runner = new FakeTraceProcessRunner(parseErrorOnFirstAttempt: true);
        var service = new FbTraceManagerProfilerSessionService(
            new TraceConfigurationBuilder(),
            new FirebirdTraceEventParser(),
            runner);

        using var secret = CredentialSecret.FromPlainText("masterkey");

        await service.StartAsync(CreateOptions(serverVersion: "2.5.9"), secret, CancellationToken.None);
        var profilerEvent = await ReadOneAsync(service);
        await service.StopAsync(CancellationToken.None);

        runner.AttemptCount.Should().Be(2);
        runner.ConfigContents[0].Should().Contain("<database employee.fdb>");
        runner.ConfigContents[1].Should().Contain("database = employee.fdb");
        profilerEvent.Type.Should().Be(TraceEventType.StatementFinished);
        profilerEvent.RawTrace.Should().NotContain("error while parsing trace configuration");
    }

    [Fact]
    public async Task StartAsync_ShouldDiscardStartupLinesAndRetry_WhenParseErrorArrivesAfterTraceStarts()
    {
        var runner = new FakeTraceProcessRunner(
            parseErrorOnFirstAttempt: true,
            emitStartupLineBeforeParse: true,
            parseErrorDelay: TimeSpan.FromMilliseconds(50));
        var service = new FbTraceManagerProfilerSessionService(
            new TraceConfigurationBuilder(),
            new FirebirdTraceEventParser(),
            runner);

        using var secret = CredentialSecret.FromPlainText("masterkey");

        await service.StartAsync(CreateOptions(serverVersion: "2.5.9"), secret, CancellationToken.None);
        var profilerEvent = await ReadOneAsync(service);
        await service.StopAsync(CancellationToken.None);

        runner.AttemptCount.Should().Be(2);
        profilerEvent.Type.Should().Be(TraceEventType.StatementFinished);
        profilerEvent.RawTrace.Should().NotContain("Trace session ID 1 started");
        profilerEvent.RawTrace.Should().NotContain("error while parsing trace configuration");
    }

    [Fact]
    public async Task StartAsync_ShouldPublishClearTechnicalEvent_WhenBothDialectsFail()
    {
        var runner = new FakeTraceProcessRunner(parseErrorOnFirstAttempt: true, parseErrorOnSecondAttempt: true);
        var service = new FbTraceManagerProfilerSessionService(
            new TraceConfigurationBuilder(),
            new FirebirdTraceEventParser(),
            runner);

        using var secret = CredentialSecret.FromPlainText("masterkey");

        await service.StartAsync(CreateOptions(serverVersion: "2.5.9"), secret, CancellationToken.None);
        var profilerEvent = await ReadOneAsync(service);

        runner.AttemptCount.Should().Be(2);
        profilerEvent.Type.Should().Be(TraceEventType.Technical);
        profilerEvent.RawTrace.Should().Contain("Configuração Trace incompatível");
        profilerEvent.RawTrace.Should().Contain("Legacy25");
        profilerEvent.RawTrace.Should().Contain("Modern30Plus");
        service.State.Should().Be(ProfilerState.Failed);
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

    private static ProfilerOptions CreateOptions(
        string serverVersion = "5.0.0",
        string? traceManagerVersion = null,
        string database = "employee.fdb")
    {
        var toolset = new EffectiveToolset(
        [
            new ToolsetCandidate(FirebirdToolKind.TraceManager, "fake-fbtracemgr.exe", traceManagerVersion, IsAvailable: true)
        ]);

        var context = new ConnectionContext(
            Guid.NewGuid(),
            "Local",
            "localhost",
            3050,
            database,
            "SYSDBA",
            FirebirdServerVersion.Parse(serverVersion),
            new FirebirdCapabilities(true, true, true, true, true, "ok"),
            toolset,
            DateTimeOffset.UtcNow);

        return new ProfilerOptions(context, "test");
    }

    private sealed class FakeTraceProcessRunner(
        int exitCode = 0,
        bool emitStatement = true,
        bool parseErrorOnFirstAttempt = false,
        bool parseErrorOnSecondAttempt = false,
        bool emitStartupLineBeforeParse = false,
        TimeSpan? parseErrorDelay = null) : ITraceProcessRunner
    {
        public TraceProcessRequest? Request { get; private set; }
        public string? FetchPath { get; private set; }
        public bool FetchFileExistedDuringRun { get; private set; }
        public string? FetchFileContentDuringRun { get; private set; }
        public int AttemptCount { get; private set; }
        public string? ConfigContentDuringRun => ConfigContents.LastOrDefault();
        public List<string> ConfigContents { get; } = [];

        public async Task<int> RunAsync(
            TraceProcessRequest request,
            Func<string, CancellationToken, Task> onOutputLine,
            Func<string, CancellationToken, Task> onErrorLine,
            CancellationToken cancellationToken)
        {
            Request = request;
            var arguments = request.Arguments.ToArray();
            var isStart = arguments.Contains("-start");
            if (!isStart)
            {
                return exitCode;
            }

            AttemptCount++;
            var configIndex = Array.IndexOf(arguments, "-config");
            if (configIndex >= 0)
            {
                ConfigContents.Add(await File.ReadAllTextAsync(arguments[configIndex + 1], cancellationToken));
            }

            var fetchIndex = Array.IndexOf(arguments, "-fetch");
            FetchPath = fetchIndex >= 0 ? arguments[fetchIndex + 1] : null;
            FetchFileExistedDuringRun = FetchPath is not null && File.Exists(FetchPath);
            FetchFileContentDuringRun = FetchPath is null ? null : await File.ReadAllTextAsync(FetchPath, cancellationToken);

            if ((AttemptCount == 1 && parseErrorOnFirstAttempt) || (AttemptCount == 2 && parseErrorOnSecondAttempt))
            {
                if (emitStartupLineBeforeParse)
                {
                    await onOutputLine("Trace session ID 1 started", cancellationToken);
                }

                if (parseErrorDelay is not null)
                {
                    await Task.Delay(parseErrorDelay.Value, cancellationToken);
                }

                await onErrorLine("error while parsing trace configuration", cancellationToken);
                await onErrorLine("line 7: expected name, got \"/\"", cancellationToken);
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }

            if (emitStatement)
            {
                await onOutputLine("statement finished", cancellationToken);
                await onOutputLine("user: SYSDBA", cancellationToken);
                await onOutputLine("attachment: 1", cancellationToken);
                await onOutputLine("transaction: 2", cancellationToken);
                await onOutputLine("duration: 1 ms", cancellationToken);
                await onOutputLine("sql: select 1 from rdb$database", cancellationToken);
                await onOutputLine(string.Empty, cancellationToken);
            }

            return exitCode;
        }
    }
}

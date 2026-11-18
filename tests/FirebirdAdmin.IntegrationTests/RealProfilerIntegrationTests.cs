using FirebirdAdmin.Application.Connections;
using FirebirdAdmin.Application.Profiler;
using FirebirdAdmin.Infrastructure.Connections;
using FirebirdAdmin.Infrastructure.Profiler;
using FirebirdSql.Data.FirebirdClient;
using FluentAssertions;

namespace FirebirdAdmin.IntegrationTests;

public sealed class RealProfilerIntegrationTests
{
    [Fact]
    public async Task Profiler_ShouldCaptureReadOnlySelect_WhenTestEnvironmentIsConfigured()
    {
        var environment = ReadEnvironment();
        if (environment is null)
        {
            return;
        }

        using var connectionPassword = CredentialSecret.FromPlainText(environment.Password);
        var connectionService = new FirebirdConnectionService(
            new FirebirdCapabilitiesResolver(),
            new FirebirdToolsetDiscoveryService());

        var context = await connectionService.ConnectAsync(
            new ConnectionRequest(environment.CreateProfile(), connectionPassword),
            CancellationToken.None);

        var profiler = new FbTraceManagerProfilerSessionService(
            new TraceConfigurationBuilder(),
            new FirebirdTraceEventParser(),
            new TraceProcessRunner());

        using var profilerPassword = CredentialSecret.FromPlainText(environment.Password);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(35));
        var stopped = false;

        try
        {
            await profiler.StartAsync(
                new ProfilerOptions(context, $"real-profiler-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}"),
                profilerPassword,
                timeout.Token);

            var readTask = WaitForStatementAsync(profiler, timeout.Token);

            await Task.Delay(TimeSpan.FromSeconds(2), timeout.Token);
            await ExecuteReadOnlySelectAsync(environment, timeout.Token);

            if (!readTask.IsCompleted)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), timeout.Token);
            }

            if (!readTask.IsCompleted)
            {
                await profiler.StopAsync(CancellationToken.None);
                stopped = true;
            }

            var profilerEvent = await readTask;

            profilerEvent.Type.Should().BeOneOf(TraceEventType.StatementStarted, TraceEventType.StatementFinished);
            profilerEvent.Sql.Should().NotBeNull();
            profilerEvent.Sql!.Should().ContainEquivalentOf("current_timestamp");
        }
        finally
        {
            if (!stopped)
            {
                await profiler.StopAsync(CancellationToken.None);
            }
        }
    }

    private static async Task<ProfilerEvent> WaitForStatementAsync(
        IProfilerSessionService profiler,
        CancellationToken cancellationToken)
    {
        var technicalEvents = new List<string>();
        var observedEvents = new List<string>();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            await foreach (var profilerEvent in profiler.ReadAllAsync(cancellationToken))
            {
                if (profilerEvent.Type is TraceEventType.Technical)
                {
                    if (!profilerEvent.RawTrace.StartsWith("Profiler Trace em execução", StringComparison.OrdinalIgnoreCase))
                    {
                        technicalEvents.Add(profilerEvent.RawTrace);
                    }

                    continue;
                }

                observedEvents.Add($"{profilerEvent.Type}: {profilerEvent.Sql ?? profilerEvent.RawTrace}");

                if (profilerEvent.Type is TraceEventType.StatementStarted or TraceEventType.StatementFinished &&
                    (profilerEvent.Sql?.Contains("current_timestamp", StringComparison.OrdinalIgnoreCase) ?? false))
                {
                    return profilerEvent;
                }

                if (stopwatch.Elapsed > TimeSpan.FromSeconds(20))
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }

        if (technicalEvents.Count > 0)
        {
            throw new InvalidOperationException(
                "SQL Profiler falhou com evento técnico: " + string.Join(Environment.NewLine, technicalEvents));
        }

        var observed = observedEvents.Count == 0
            ? "Nenhum evento chegou ao stream."
            : string.Join(Environment.NewLine, observedEvents.TakeLast(10));

        throw new TimeoutException("Trace iniciou, mas nenhum statement foi capturado em até 35 segundos. " + observed);
    }

    private static async Task ExecuteReadOnlySelectAsync(
        FirebirdTestEnvironment environment,
        CancellationToken cancellationToken)
    {
        await using var connection = new FbConnection(BuildConnectionString(environment));
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "select current_timestamp from rdb$database";
        _ = await command.ExecuteScalarAsync(cancellationToken);
    }

    private static string BuildConnectionString(FirebirdTestEnvironment environment)
    {
        return new FbConnectionStringBuilder
        {
            DataSource = environment.Host,
            Port = environment.Port,
            Database = environment.Database,
            UserID = environment.User,
            Password = environment.Password,
            Pooling = false
        }.ToString();
    }

    private static FirebirdTestEnvironment? ReadEnvironment()
    {
        var host = Environment.GetEnvironmentVariable("FIREBIRDADMIN_TEST_HOST");
        var database = Environment.GetEnvironmentVariable("FIREBIRDADMIN_TEST_DATABASE");
        var user = Environment.GetEnvironmentVariable("FIREBIRDADMIN_TEST_USER");
        var password = Environment.GetEnvironmentVariable("FIREBIRDADMIN_TEST_PASSWORD");

        if (string.IsNullOrWhiteSpace(host) ||
            string.IsNullOrWhiteSpace(database) ||
            string.IsNullOrWhiteSpace(user) ||
            string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        var port = int.TryParse(Environment.GetEnvironmentVariable("FIREBIRDADMIN_TEST_PORT"), out var parsedPort)
            ? parsedPort
            : 3050;

        return new FirebirdTestEnvironment(host, port, database, user, password);
    }

    private sealed record FirebirdTestEnvironment(
        string Host,
        int Port,
        string Database,
        string User,
        string Password)
    {
        public ConnectionProfile CreateProfile()
        {
            return new ConnectionProfile(
                Guid.NewGuid(),
                "RealProfiler",
                Host,
                Port,
                Database,
                User,
                "UTF8",
                null,
                HasSavedPassword: false,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow);
        }
    }
}

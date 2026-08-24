using FirebirdAdmin.Application.Connections;
using FirebirdAdmin.Application.Profiler;
using FirebirdAdmin.Infrastructure.Profiler;
using FluentAssertions;

namespace FirebirdAdmin.Infrastructure.Tests;

public sealed class TraceConfigurationBuilderTests
{
    [Fact]
    public void Build_ShouldGenerateLegacyConfigForFirebird25()
    {
        var builder = new TraceConfigurationBuilder();

        var config = builder.Build(CreateOptions("2.5.9"), FirebirdServerVersion.Parse("2.5.9"));

        config.Should().Contain("<database employee.fdb>");
        config.Should().Contain("enabled true");
        config.Should().Contain("log_statement_finish true");
    }

    [Fact]
    public void Build_ShouldGenerateModernConfigForFirebird3Plus()
    {
        var builder = new TraceConfigurationBuilder();

        var config = builder.Build(CreateOptions("5.0.0"), FirebirdServerVersion.Parse("5.0.0"));

        config.Should().Contain("database = employee.fdb");
        config.Should().Contain("enabled = true");
        config.Should().Contain("print_perf = true");
    }

    private static ProfilerOptions CreateOptions(string version)
    {
        var context = new ConnectionContext(
            Guid.NewGuid(),
            "Local",
            "localhost",
            3050,
            "employee.fdb",
            "SYSDBA",
            FirebirdServerVersion.Parse(version),
            new FirebirdCapabilities(true, true, true, true, true, "ok"),
            EffectiveToolset.Empty,
            DateTimeOffset.UtcNow);

        return new ProfilerOptions(context, "test");
    }
}

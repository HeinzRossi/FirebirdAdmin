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
        config.Should().Contain("</database>");
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

    [Fact]
    public void Build_ShouldGenerateModernConfigForExplicitModernDialect()
    {
        var builder = new TraceConfigurationBuilder();
        var options = CreateOptions("2.5.9", @"E:\DESENVOLVIMENTOGIT\RICS_BR\EXECUÇÃO\DB\RICS.GDB");

        var config = builder.Build(options, TraceConfigurationDialect.Modern30Plus);

        config.Should().Contain("database = RICS.GDB");
        config.Should().Contain("{");
        config.Should().Contain("enabled = true");
        config.Should().NotContain("</database>");
    }

    [Fact]
    public void Build_ShouldNotPlaceWindowsPathInsideLegacyDatabaseTagForFirebird25()
    {
        var builder = new TraceConfigurationBuilder();
        var options = CreateOptions("2.5.9", @"E:\DESENVOLVIMENTOGIT\RICS_BR\EXECUÇÃO\DB\RICS.GDB");

        var config = builder.Build(options, FirebirdServerVersion.Parse("2.5.9"));

        config.Should().Contain("<database %RICS.GDB>");
        config.Should().Contain("</database>");
        config.Should().NotContain(@"E:\DESENVOLVIMENTOGIT");
        config.Split(Environment.NewLine)[0].Should().NotContain(@"\");
        config.Split(Environment.NewLine)[0].Should().NotContain("/");
    }

    [Fact]
    public void Build_ShouldUseAliasAsLegacyDatabaseTargetForFirebird25()
    {
        var builder = new TraceConfigurationBuilder();
        var options = CreateOptions("2.5.9", "employee");

        var config = builder.Build(options, FirebirdServerVersion.Parse("2.5.9"));

        config.Should().Contain("<database employee>");
    }

    private static ProfilerOptions CreateOptions(string version, string database = "employee.fdb")
    {
        var context = new ConnectionContext(
            Guid.NewGuid(),
            "Local",
            "localhost",
            3050,
            database,
            "SYSDBA",
            FirebirdServerVersion.Parse(version),
            new FirebirdCapabilities(true, true, true, true, true, "ok"),
            EffectiveToolset.Empty,
            DateTimeOffset.UtcNow);

        return new ProfilerOptions(context, "test");
    }
}

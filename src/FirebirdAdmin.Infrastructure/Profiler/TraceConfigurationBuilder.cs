using System.Globalization;
using FirebirdAdmin.Application.Connections;
using FirebirdAdmin.Application.Profiler;

namespace FirebirdAdmin.Infrastructure.Profiler;

public sealed class TraceConfigurationBuilder : ITraceConfigurationBuilder
{
    public string Build(ProfilerOptions options, FirebirdServerVersion serverVersion)
    {
        var threshold = options.SlowQueryThreshold?.TotalMilliseconds ?? 0;
        var thresholdText = ((long)threshold).ToString(CultureInfo.InvariantCulture);

        return serverVersion.Major <= 2
            ? BuildLegacy(GetLegacyDatabaseTarget(options.Connection.Database), thresholdText)
            : BuildModern(options.Connection.Database, thresholdText);
    }

    private static string BuildLegacy(string databaseTarget, string threshold)
    {
        return string.Join(
            Environment.NewLine,
            $"<database {databaseTarget}>",
            "  enabled true",
            "  log_statement_start true",
            "  log_statement_finish true",
            "  print_plan true",
            "  print_perf true",
            $"  time_threshold {threshold}",
            "</database>",
            string.Empty);
    }

    private static string GetLegacyDatabaseTarget(string database)
    {
        if (string.IsNullOrWhiteSpace(database))
        {
            return "*";
        }

        var normalized = database.Trim();
        if (!normalized.Contains('\\') && !normalized.Contains('/'))
        {
            return normalized;
        }

        normalized = normalized.Replace('/', Path.DirectorySeparatorChar);
        var fileName = Path.GetFileName(normalized);
        return string.IsNullOrWhiteSpace(fileName) ? "*" : fileName;
    }

    private static string BuildModern(string database, string threshold)
    {
        return string.Join(
            Environment.NewLine,
            $"database = {database}",
            "{",
            "  enabled = true",
            "  log_statement_start = true",
            "  log_statement_finish = true",
            "  print_plan = true",
            "  print_perf = true",
            $"  time_threshold = {threshold}",
            "}",
            string.Empty);
    }
}

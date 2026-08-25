using System.Globalization;
using FirebirdAdmin.Application.Connections;
using FirebirdAdmin.Application.Profiler;

namespace FirebirdAdmin.Infrastructure.Profiler;

public sealed class TraceConfigurationBuilder : ITraceConfigurationBuilder
{
    public string Build(ProfilerOptions options, FirebirdServerVersion serverVersion)
    {
        return Build(
            options,
            serverVersion.Major <= 2
                ? TraceConfigurationDialect.Legacy25
                : TraceConfigurationDialect.Modern30Plus);
    }

    public string Build(ProfilerOptions options, TraceConfigurationDialect dialect)
    {
        var threshold = options.SlowQueryThreshold?.TotalMilliseconds ?? 0;
        var thresholdText = ((long)threshold).ToString(CultureInfo.InvariantCulture);

        return dialect is TraceConfigurationDialect.Legacy25
            ? BuildLegacy(GetLegacyDatabaseTarget(options.Connection.Database), thresholdText)
            : BuildModern(GetModernDatabaseTarget(options.Connection.Database), thresholdText);
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
        return GetDatabaseFileNameOrAlias(database, usePathSuffixPattern: true);
    }

    private static string GetModernDatabaseTarget(string database)
    {
        return GetDatabaseFileNameOrAlias(database, usePathSuffixPattern: false);
    }

    private static string GetDatabaseFileNameOrAlias(string database, bool usePathSuffixPattern)
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
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return "*";
        }

        return usePathSuffixPattern ? $"%{fileName}" : fileName;
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

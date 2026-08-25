using System.Globalization;
using System.Text.RegularExpressions;

namespace FirebirdAdmin.Application.Profiler;

public sealed class FirebirdTraceEventParser : ITraceEventParser
{
    private static readonly Regex AttachmentRegex = new(@"(?:attachment|att(?:achment)?_id)\s*[:=]\s*(?<value>\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TransactionRegex = new(@"(?:transaction|tra(?:nsaction)?_id)\s*[:=]\s*(?<value>\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex UserRegex = new(@"(?:user|username)\s*[:=]\s*(?<value>[A-Za-z0-9_$.\-]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DurationRegex = new(@"(?:duration|time)\s*[:=]\s*(?<value>\d+(?:[.,]\d+)?)\s*(?<unit>ms|s)?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ReadsRegex = new(@"reads\s*[:=]\s*(?<value>\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex WritesRegex = new(@"writes\s*[:=]\s*(?<value>\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex FetchesRegex = new(@"fetches\s*[:=]\s*(?<value>\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex MarksRegex = new(@"marks\s*[:=]\s*(?<value>\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public IReadOnlyList<ProfilerEvent> ParseBlock(string block, long startingSequence, DateTimeOffset timestamp)
    {
        if (string.IsNullOrWhiteSpace(block))
        {
            return [];
        }

        var type = ResolveType(block);
        if (type is TraceEventType.Unparsed)
        {
            return [CreateUnparsed(startingSequence, timestamp, block)];
        }

        var sql = ExtractSql(block);
        var plan = ExtractPlan(block);

        return
        [
            new ProfilerEvent(
                startingSequence,
                timestamp,
                type,
                ExtractDuration(block),
                ExtractString(UserRegex, block),
                ExtractLong(AttachmentRegex, block),
                ExtractLong(TransactionRegex, block),
                sql,
                new ProfilerMetrics(
                    ExtractLong(ReadsRegex, block),
                    ExtractLong(WritesRegex, block),
                    ExtractLong(FetchesRegex, block),
                    ExtractLong(MarksRegex, block)),
                plan,
                block)
        ];
    }

    private static ProfilerEvent CreateUnparsed(long sequence, DateTimeOffset timestamp, string block)
    {
        return new ProfilerEvent(
            sequence,
            timestamp,
            TraceEventType.Unparsed,
            null,
            null,
            null,
            null,
            null,
            new ProfilerMetrics(),
            null,
            block);
    }

    private static TraceEventType ResolveType(string block)
    {
        if (block.Contains("EXECUTE_STATEMENT_FINISH", StringComparison.OrdinalIgnoreCase) ||
            block.Contains("EXECUTE_STATEMENT_FINISHED", StringComparison.OrdinalIgnoreCase))
        {
            return TraceEventType.StatementFinished;
        }

        if (block.Contains("EXECUTE_STATEMENT_START", StringComparison.OrdinalIgnoreCase) ||
            block.Contains("PREPARE_STATEMENT", StringComparison.OrdinalIgnoreCase))
        {
            return TraceEventType.StatementStarted;
        }

        if (block.Contains("statement", StringComparison.OrdinalIgnoreCase) &&
            (block.Contains("finish", StringComparison.OrdinalIgnoreCase) ||
             block.Contains("finished", StringComparison.OrdinalIgnoreCase) ||
             block.Contains("close", StringComparison.OrdinalIgnoreCase)))
        {
            return TraceEventType.StatementFinished;
        }

        if (block.Contains("statement", StringComparison.OrdinalIgnoreCase) &&
            (block.Contains("start", StringComparison.OrdinalIgnoreCase) ||
             block.Contains("started", StringComparison.OrdinalIgnoreCase) ||
             block.Contains("prepare", StringComparison.OrdinalIgnoreCase)))
        {
            return TraceEventType.StatementStarted;
        }

        return TraceEventType.Unparsed;
    }

    private static string? ExtractSql(string block)
    {
        var lines = block.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var sqlLine = lines.FirstOrDefault(line => line.StartsWith("sql:", StringComparison.OrdinalIgnoreCase));
        if (sqlLine is not null)
        {
            return sqlLine[4..].Trim();
        }

        var statementIndex = Array.FindIndex(lines, line => line.Contains("statement", StringComparison.OrdinalIgnoreCase));
        if (statementIndex >= 0 && statementIndex + 1 < lines.Length)
        {
            var candidate = FindSqlCandidate(lines, statementIndex + 1);
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static string FindSqlCandidate(string[] lines, int startIndex)
    {
        for (var index = startIndex; index < lines.Length; index++)
        {
            var candidate = lines[index];
            if (candidate.Contains("select", StringComparison.OrdinalIgnoreCase) ||
                candidate.Contains("insert", StringComparison.OrdinalIgnoreCase) ||
                candidate.Contains("update", StringComparison.OrdinalIgnoreCase) ||
                candidate.Contains("delete", StringComparison.OrdinalIgnoreCase) ||
                candidate.Contains("execute", StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        return string.Empty;
    }

    private static string? ExtractPlan(string block)
    {
        var lines = block.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return lines.FirstOrDefault(line => line.StartsWith("plan", StringComparison.OrdinalIgnoreCase));
    }

    private static string? ExtractString(Regex regex, string text)
    {
        var match = regex.Match(text);
        return match.Success ? match.Groups["value"].Value : null;
    }

    private static long? ExtractLong(Regex regex, string text)
    {
        var value = ExtractString(regex, text);
        return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    }

    private static TimeSpan? ExtractDuration(string text)
    {
        var match = DurationRegex.Match(text);
        if (!match.Success)
        {
            return null;
        }

        var raw = match.Groups["value"].Value.Replace(',', '.');
        if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            return null;
        }

        var unit = match.Groups["unit"].Value;
        return string.Equals(unit, "s", StringComparison.OrdinalIgnoreCase)
            ? TimeSpan.FromSeconds(value)
            : TimeSpan.FromMilliseconds(value);
    }
}

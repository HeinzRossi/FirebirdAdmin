using System.Text;
using System.Text.Json;
using FirebirdAdmin.Application.History;
using FirebirdAdmin.Infrastructure.Persistence;
using FirebirdAdmin.Infrastructure.Security;

namespace FirebirdAdmin.Infrastructure.History;

public sealed class HistoryExportService(
    IHistoryQueryService queryService,
    ApplicationDataPaths paths) : IHistoryExportService
{
    public async Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(paths.ExportDirectory);
        var outputPath = request.OutputPath ?? Path.Combine(
            paths.ExportDirectory,
            $"history-{DateTimeOffset.Now:yyyyMMdd-HHmmss}.{request.Format.ToString().ToLowerInvariant()}");
        var outputDirectory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        return request.Query.Kind is HistoryDataKind.MonitoringSnapshots
            ? await ExportMonitoringAsync(request, outputPath, cancellationToken)
            : await ExportTraceAsync(request, outputPath, cancellationToken);
    }

    private async Task<ExportResult> ExportTraceAsync(ExportRequest request, string outputPath, CancellationToken cancellationToken)
    {
        if (request.Format is ExportFormat.Json)
        {
            return await ExportTraceJsonAsync(request, outputPath, cancellationToken);
        }

        var rowCount = 0;
        await using var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 65536, useAsync: true);
        await using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        await writer.WriteLineAsync("Id,Timestamp,Type,DurationMs,UserName,AttachmentId,TransactionId,Sql");

        await foreach (var item in ReadTraceItemsAsync(request.Query, cancellationToken))
        {
            var sanitized = Sanitize(item);
            await writer.WriteLineAsync(string.Join(
                ',',
                sanitized.Id,
                sanitized.Timestamp.ToString("O"),
                Escape(sanitized.Type.ToString()),
                sanitized.Duration?.TotalMilliseconds.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
                Escape(sanitized.UserName),
                sanitized.AttachmentId?.ToString() ?? string.Empty,
                sanitized.TransactionId?.ToString() ?? string.Empty,
                Escape(sanitized.Sql)));
            rowCount++;
        }

        return new ExportResult(outputPath, rowCount);
    }

    private async Task<ExportResult> ExportMonitoringAsync(ExportRequest request, string outputPath, CancellationToken cancellationToken)
    {
        if (request.Format is ExportFormat.Json)
        {
            return await ExportMonitoringJsonAsync(request, outputPath, cancellationToken);
        }

        var rowCount = 0;
        await using var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 65536, useAsync: true);
        await using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        await writer.WriteLineAsync("Id,CapturedAt,AttachmentCount,TransactionCount,StatementCount");

        await foreach (var item in ReadMonitoringItemsAsync(request.Query, cancellationToken))
        {
            await writer.WriteLineAsync($"{item.Id},{item.CapturedAt:O},{item.AttachmentCount},{item.TransactionCount},{item.StatementCount}");
            rowCount++;
        }

        return new ExportResult(outputPath, rowCount);
    }

    private static string Escape(string? value)
    {
        var masked = SecretMasker.MaskSecrets(value ?? string.Empty);
        if (masked.TrimStart().StartsWith('=') ||
            masked.TrimStart().StartsWith('+') ||
            masked.TrimStart().StartsWith('-') ||
            masked.TrimStart().StartsWith('@'))
        {
            masked = $"'{masked}";
        }

        return $"\"{masked.Replace("\"", "\"\"")}\"";
    }

    private async Task<ExportResult> ExportTraceJsonAsync(ExportRequest request, string outputPath, CancellationToken cancellationToken)
    {
        var rowCount = 0;
        await using var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 65536, useAsync: true);
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
        writer.WriteStartArray();
        await foreach (var item in ReadTraceItemsAsync(request.Query, cancellationToken))
        {
            JsonSerializer.Serialize(writer, Sanitize(item));
            rowCount++;
        }

        writer.WriteEndArray();
        await writer.FlushAsync(cancellationToken);
        return new ExportResult(outputPath, rowCount);
    }

    private async Task<ExportResult> ExportMonitoringJsonAsync(ExportRequest request, string outputPath, CancellationToken cancellationToken)
    {
        var rowCount = 0;
        await using var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 65536, useAsync: true);
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
        writer.WriteStartArray();
        await foreach (var item in ReadMonitoringItemsAsync(request.Query, cancellationToken))
        {
            JsonSerializer.Serialize(writer, item);
            rowCount++;
        }

        writer.WriteEndArray();
        await writer.FlushAsync(cancellationToken);
        return new ExportResult(outputPath, rowCount);
    }

    private async IAsyncEnumerable<TraceEventHistoryItem> ReadTraceItemsAsync(
        HistoryQuery query,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        const int pageSize = 500;
        var pageNumber = 1;
        long emitted = 0;

        while (true)
        {
            var page = await queryService.QueryTraceEventsAsync(query with { Page = pageNumber, PageSize = pageSize }, cancellationToken);
            foreach (var item in page.Items)
            {
                yield return item;
                emitted++;
            }

            if (page.Items.Count == 0 || emitted >= page.TotalCount)
            {
                yield break;
            }

            pageNumber++;
        }
    }

    private async IAsyncEnumerable<MonitoringSnapshotHistoryItem> ReadMonitoringItemsAsync(
        HistoryQuery query,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        const int pageSize = 500;
        var pageNumber = 1;
        long emitted = 0;

        while (true)
        {
            var page = await queryService.QueryMonitoringSnapshotsAsync(query with { Page = pageNumber, PageSize = pageSize }, cancellationToken);
            foreach (var item in page.Items)
            {
                yield return item;
                emitted++;
            }

            if (page.Items.Count == 0 || emitted >= page.TotalCount)
            {
                yield break;
            }

            pageNumber++;
        }
    }

    private static TraceEventHistoryItem Sanitize(TraceEventHistoryItem item)
    {
        return item with
        {
            UserName = SecretMasker.MaskSecrets(item.UserName ?? string.Empty),
            Sql = SecretMasker.MaskSecrets(item.Sql ?? string.Empty),
            Plan = SecretMasker.MaskSecrets(item.Plan ?? string.Empty),
            RawTrace = SecretMasker.MaskSecrets(item.RawTrace)
        };
    }
}

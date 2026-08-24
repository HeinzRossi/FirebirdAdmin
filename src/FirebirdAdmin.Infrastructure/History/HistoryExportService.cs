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

        return request.Query.Kind is HistoryDataKind.MonitoringSnapshots
            ? await ExportMonitoringAsync(request, outputPath, cancellationToken)
            : await ExportTraceAsync(request, outputPath, cancellationToken);
    }

    private async Task<ExportResult> ExportTraceAsync(ExportRequest request, string outputPath, CancellationToken cancellationToken)
    {
        var page = await queryService.QueryTraceEventsAsync(request.Query with { Page = 1, PageSize = 500 }, cancellationToken);
        if (request.Format is ExportFormat.Json)
        {
            await File.WriteAllTextAsync(outputPath, JsonSerializer.Serialize(page.Items), cancellationToken);
            return new ExportResult(outputPath, page.Items.Count);
        }

        var builder = new StringBuilder();
        builder.AppendLine("Id,Timestamp,Type,DurationMs,UserName,AttachmentId,TransactionId,Sql");
        foreach (var item in page.Items)
        {
            builder.AppendLine(string.Join(
                ',',
                item.Id,
                item.Timestamp.ToString("O"),
                Escape(item.Type.ToString()),
                item.Duration?.TotalMilliseconds.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
                Escape(item.UserName),
                item.AttachmentId?.ToString() ?? string.Empty,
                item.TransactionId?.ToString() ?? string.Empty,
                Escape(item.Sql)));
        }

        await File.WriteAllTextAsync(outputPath, builder.ToString(), cancellationToken);
        return new ExportResult(outputPath, page.Items.Count);
    }

    private async Task<ExportResult> ExportMonitoringAsync(ExportRequest request, string outputPath, CancellationToken cancellationToken)
    {
        var page = await queryService.QueryMonitoringSnapshotsAsync(request.Query with { Page = 1, PageSize = 500 }, cancellationToken);
        if (request.Format is ExportFormat.Json)
        {
            await File.WriteAllTextAsync(outputPath, JsonSerializer.Serialize(page.Items), cancellationToken);
            return new ExportResult(outputPath, page.Items.Count);
        }

        var builder = new StringBuilder();
        builder.AppendLine("Id,CapturedAt,AttachmentCount,TransactionCount,StatementCount");
        foreach (var item in page.Items)
        {
            builder.AppendLine($"{item.Id},{item.CapturedAt:O},{item.AttachmentCount},{item.TransactionCount},{item.StatementCount}");
        }

        await File.WriteAllTextAsync(outputPath, builder.ToString(), cancellationToken);
        return new ExportResult(outputPath, page.Items.Count);
    }

    private static string Escape(string? value)
    {
        var masked = SecretMasker.MaskSecrets(value ?? string.Empty);
        return $"\"{masked.Replace("\"", "\"\"")}\"";
    }
}

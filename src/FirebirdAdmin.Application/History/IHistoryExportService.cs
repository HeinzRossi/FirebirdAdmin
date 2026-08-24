namespace FirebirdAdmin.Application.History;

public interface IHistoryExportService
{
    Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken);
}

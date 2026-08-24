namespace FirebirdAdmin.Application.History;

public sealed record ExportRequest(
    HistoryQuery Query,
    ExportFormat Format,
    string? OutputPath = null);

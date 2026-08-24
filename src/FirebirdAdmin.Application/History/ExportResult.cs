namespace FirebirdAdmin.Application.History;

public sealed record ExportResult(
    string OutputPath,
    long RowCount);

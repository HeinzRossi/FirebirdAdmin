namespace FirebirdAdmin.Application.Metadata;

public sealed record MetadataColumn(
    string Name,
    string DataType,
    bool IsNullable,
    int Position,
    string? DefaultSource = null);

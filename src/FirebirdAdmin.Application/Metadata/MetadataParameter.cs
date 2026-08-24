namespace FirebirdAdmin.Application.Metadata;

public sealed record MetadataParameter(
    string Name,
    string DataType,
    int Position,
    string Direction);

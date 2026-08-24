namespace FirebirdAdmin.Application.Metadata;

public sealed record MetadataIndex(
    string Name,
    bool IsUnique,
    IReadOnlyList<string> Columns);

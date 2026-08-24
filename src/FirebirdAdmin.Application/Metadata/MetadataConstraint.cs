namespace FirebirdAdmin.Application.Metadata;

public sealed record MetadataConstraint(
    string Name,
    string Type,
    IReadOnlyList<string> Columns);

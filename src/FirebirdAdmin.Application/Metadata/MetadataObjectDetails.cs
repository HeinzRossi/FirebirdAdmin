namespace FirebirdAdmin.Application.Metadata;

public sealed record MetadataObjectDetails(
    MetadataObjectSummary Summary,
    IReadOnlyList<MetadataColumn> Columns,
    IReadOnlyList<MetadataParameter> Parameters,
    IReadOnlyList<MetadataIndex> Indexes,
    IReadOnlyList<MetadataConstraint> Constraints,
    IReadOnlyList<MetadataTrigger> Triggers,
    IReadOnlyList<MetadataDependency> Dependencies,
    string? Source,
    string? Ddl,
    string? Error = null);

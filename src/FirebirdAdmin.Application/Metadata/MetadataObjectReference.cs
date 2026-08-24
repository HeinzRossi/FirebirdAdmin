namespace FirebirdAdmin.Application.Metadata;

public sealed record MetadataObjectReference(
    MetadataObjectKind Kind,
    string Name);

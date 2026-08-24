namespace FirebirdAdmin.Application.Metadata;

public sealed record MetadataDependency(
    MetadataObjectReference Reference,
    string Direction);

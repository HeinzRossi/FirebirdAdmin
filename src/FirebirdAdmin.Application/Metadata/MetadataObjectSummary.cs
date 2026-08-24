namespace FirebirdAdmin.Application.Metadata;

public sealed record MetadataObjectSummary(
    MetadataObjectReference Reference,
    string DisplayName,
    bool IsSystemObject = false,
    string? Description = null);

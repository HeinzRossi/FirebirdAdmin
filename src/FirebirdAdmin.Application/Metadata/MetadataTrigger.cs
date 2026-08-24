namespace FirebirdAdmin.Application.Metadata;

public sealed record MetadataTrigger(
    string Name,
    bool IsActive,
    string? Source);

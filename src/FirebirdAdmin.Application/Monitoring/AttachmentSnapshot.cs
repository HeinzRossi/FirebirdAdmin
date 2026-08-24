namespace FirebirdAdmin.Application.Monitoring;

public sealed record AttachmentSnapshot(
    long AttachmentId,
    string? UserName,
    string? RemoteAddress,
    string? RemoteProcess,
    DateTimeOffset? ConnectedAt,
    string? State);

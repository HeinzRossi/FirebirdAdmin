namespace FirebirdAdmin.Application.History;

public sealed record RetentionPolicy(
    int RetentionDays = 30,
    long MaxDatabaseBytes = 5L * 1024 * 1024 * 1024,
    int BatchSize = 500);

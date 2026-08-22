namespace FirebirdAdmin.Application.Connections;

public sealed record FirebirdCapabilities(
    bool SupportsTrace,
    bool SupportsPackages,
    bool SupportsStandaloneFunctions,
    bool SupportsIdentityColumns,
    bool SupportsSqlSecurity,
    string Explanation);

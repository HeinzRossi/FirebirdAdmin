using FirebirdAdmin.Application.Connections;

namespace FirebirdAdmin.Infrastructure.Connections;

public sealed class FirebirdCapabilitiesResolver : IFirebirdCapabilitiesResolver
{
    public FirebirdCapabilities Resolve(FirebirdServerVersion version)
    {
        if (version.Major <= 0)
        {
            return new FirebirdCapabilities(
                SupportsTrace: false,
                SupportsPackages: false,
                SupportsStandaloneFunctions: false,
                SupportsIdentityColumns: false,
                SupportsSqlSecurity: false,
                Explanation: "Versao Firebird desconhecida; recursos ficam desabilitados de forma conservadora.");
        }

        return new FirebirdCapabilities(
            SupportsTrace: true,
            SupportsPackages: version.Major >= 3,
            SupportsStandaloneFunctions: version.Major >= 3,
            SupportsIdentityColumns: version.Major >= 3,
            SupportsSqlSecurity: version.Major >= 4,
            Explanation: $"Capabilities resolvidas para Firebird {version.Major}.{version.Minor}.");
    }
}

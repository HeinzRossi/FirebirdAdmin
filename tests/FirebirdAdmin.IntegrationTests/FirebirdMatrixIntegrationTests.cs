using FirebirdAdmin.Application.Connections;
using FirebirdAdmin.Application.Metadata;
using FirebirdAdmin.Application.Monitoring;
using FirebirdAdmin.Application.Profiler;
using FirebirdAdmin.Application.Security;
using FirebirdAdmin.Infrastructure.Connections;
using FirebirdAdmin.Infrastructure.Metadata;
using FirebirdAdmin.Infrastructure.Monitoring;
using FirebirdAdmin.Infrastructure.Profiler;
using FirebirdAdmin.Infrastructure.Security;
using FluentAssertions;

namespace FirebirdAdmin.IntegrationTests;

public sealed class FirebirdMatrixIntegrationTests
{
    [Fact]
    public async Task Connection_ShouldDetectExpectedMajorVersion_WhenMatrixEnvironmentIsConfigured()
    {
        foreach (var versionCase in FirebirdVersionTestEnvironment.ReadConfiguredCases())
        {
            using var password = CredentialSecret.FromPlainText(versionCase.Password);
            var service = new FirebirdConnectionService(new FirebirdCapabilitiesResolver(), new FirebirdToolsetDiscoveryService());

            var context = await service.ConnectAsync(new ConnectionRequest(versionCase.CreateProfile(), password), CancellationToken.None);

            context.ServerVersion.Major.Should().Be(versionCase.ExpectedMajor, versionCase.Key);
            context.Capabilities.SupportsTrace.Should().BeTrue(versionCase.Key);
        }
    }

    [Fact]
    public async Task Monitoring_ShouldCaptureSnapshot_WhenMatrixEnvironmentIsConfigured()
    {
        foreach (var versionCase in FirebirdVersionTestEnvironment.ReadConfiguredCases())
        {
            using var password = CredentialSecret.FromPlainText(versionCase.Password);
            var strategy = new FirebirdMonitoringQueryStrategy();

            var snapshot = await strategy.CaptureAsync(Guid.NewGuid(), versionCase.CreateProfile(), password, CancellationToken.None);

            snapshot.CapturedAt.Should().BeBefore(DateTimeOffset.UtcNow.AddSeconds(1), versionCase.Key);
            snapshot.Attachments.Should().NotBeNull(versionCase.Key);
            snapshot.Transactions.Should().NotBeNull(versionCase.Key);
            snapshot.Statements.Should().NotBeNull(versionCase.Key);
        }
    }

    [Fact]
    public async Task MetadataAndSecurity_ShouldLoadCatalogOrReturnClearPermissionError_WhenMatrixEnvironmentIsConfigured()
    {
        foreach (var versionCase in FirebirdVersionTestEnvironment.ReadConfiguredCases())
        {
            using var password = CredentialSecret.FromPlainText(versionCase.Password);
            var context = await ConnectAsync(versionCase, password);

            var metadata = await new MetadataCatalogService(
                    new FirebirdMetadataQueryStrategy(new MetadataDdlBuilder()),
                    new MetadataCache())
                .LoadCatalogAsync(context, password, CancellationToken.None);
            metadata.Objects.Should().NotBeNull(versionCase.Key);

            var security = await new SecurityCatalogService(
                    new FirebirdSecurityQueryStrategy(),
                    new SecurityCache())
                .LoadCatalogAsync(context, password, CancellationToken.None);
            (security.Error is null || security.Error.Length > 0).Should().BeTrue(versionCase.Key);
        }
    }

    [Fact]
    public async Task TraceConfiguration_ShouldUseExpectedSyntax_WhenMatrixEnvironmentIsConfigured()
    {
        foreach (var versionCase in FirebirdVersionTestEnvironment.ReadConfiguredCases())
        {
            using var password = CredentialSecret.FromPlainText(versionCase.Password);
            var context = await ConnectAsync(versionCase, password);
            var config = new TraceConfigurationBuilder().Build(new ProfilerOptions(context, $"matrix-{versionCase.Key}"), context.ServerVersion);

            if (versionCase.ExpectedMajor <= 2)
            {
                config.Should().Contain("<database", versionCase.Key);
                config.Should().Contain("enabled true", versionCase.Key);
            }
            else
            {
                config.Should().Contain("database =", versionCase.Key);
                config.Should().Contain("enabled = true", versionCase.Key);
            }
        }
    }

    private static async Task<ConnectionContext> ConnectAsync(FirebirdVersionCase versionCase, CredentialSecret password)
    {
        var service = new FirebirdConnectionService(new FirebirdCapabilitiesResolver(), new FirebirdToolsetDiscoveryService());
        return await service.ConnectAsync(new ConnectionRequest(versionCase.CreateProfile(), password), CancellationToken.None);
    }
}

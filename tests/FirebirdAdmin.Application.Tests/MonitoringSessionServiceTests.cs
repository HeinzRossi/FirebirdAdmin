using FirebirdAdmin.Application.Connections;
using FirebirdAdmin.Application.Monitoring;
using FluentAssertions;

namespace FirebirdAdmin.Application.Tests;

public sealed class MonitoringSessionServiceTests
{
    [Fact]
    public async Task StartAsync_ShouldPublishSnapshotsFromStrategy()
    {
        var strategy = new FakeMonitoringQueryStrategy();
        var service = new MonitoringSessionService(strategy);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        using var password = CredentialSecret.FromPlainText("masterkey");
        var connection = CreateConnectionContext();
        var profile = CreateProfile();

        await service.StartAsync(connection, profile, password, PollingOptions.Aggressive, cts.Token);

        var snapshot = await service.ReadAllAsync(cts.Token).FirstAsync(cts.Token);

        snapshot.Transactions.Should().ContainSingle(transaction => transaction.TransactionId == 10);
        service.Status.State.Should().Be(PollingState.Connected);
        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_ShouldEnterReconnectingWhenStrategyFails()
    {
        var strategy = new FakeMonitoringQueryStrategy(shouldFail: true);
        var service = new MonitoringSessionService(strategy);
        using var cts = new CancellationTokenSource();

        await service.StartAsync(CreateConnectionContext(), CreateProfile(), null, PollingOptions.Aggressive, cts.Token);
        await Task.Delay(600, CancellationToken.None);

        service.Status.State.Should().Be(PollingState.Reconnecting);
        await service.StopAsync(CancellationToken.None);
    }

    private static ConnectionProfile CreateProfile()
    {
        return new ConnectionProfile(Guid.NewGuid(), "Local", "localhost", 3050, "employee.fdb", "SYSDBA", "UTF8", null, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
    }

    private static ConnectionContext CreateConnectionContext()
    {
        return new ConnectionContext(
            Guid.NewGuid(),
            "Local",
            "localhost",
            3050,
            "employee.fdb",
            "SYSDBA",
            FirebirdServerVersion.Parse("5.0.0"),
            new FirebirdCapabilities(true, true, true, true, true, "ok"),
            EffectiveToolset.Empty,
            DateTimeOffset.UtcNow);
    }

    private sealed class FakeMonitoringQueryStrategy(bool shouldFail = false) : IMonitoringQueryStrategy
    {
        public Task<MonitoringSnapshot> CaptureAsync(Guid sessionId, ConnectionProfile profile, CredentialSecret? password, CancellationToken cancellationToken)
        {
            if (shouldFail)
            {
                throw new InvalidOperationException("MON$ unavailable");
            }

            return Task.FromResult(new MonitoringSnapshot(
                sessionId,
                DateTimeOffset.UtcNow,
                [],
                [new TransactionSnapshot(10, 20, "1", DateTimeOffset.UtcNow, 1, 2, 3, 4)],
                []));
        }
    }
}

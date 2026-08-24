using FirebirdAdmin.Application.Monitoring;

namespace FirebirdAdmin.Application.Dashboard;

public sealed class DashboardProjectionService : IDashboardProjectionService
{
    public DashboardSnapshot CreateDisconnected()
    {
        return new DashboardSnapshot(
            DatabaseHealthStatus.Disconnected,
            "Conecte a um banco para iniciar o dashboard operacional.",
            null,
            CreateMetrics(0, 0, 0, null),
            []);
    }

    public DashboardSnapshot Project(MonitoringSnapshot snapshot, DateTimeOffset now)
    {
        var activeStatements = snapshot.Statements.Count(statement => statement.State is not "0");
        var health = ResolveHealth(snapshot, now);
        var message = health switch
        {
            DatabaseHealthStatus.Stale => "Dados desatualizados. Verifique conexão e polling.",
            DatabaseHealthStatus.Warning => "Snapshot recebido com informações incompletas.",
            _ => "Banco respondendo ao monitoramento MON$."
        };

        return new DashboardSnapshot(
            health,
            message,
            snapshot.CapturedAt,
            CreateMetrics(
                snapshot.Attachments.Count,
                snapshot.Transactions.Count,
                activeStatements,
                snapshot.Transactions.Select(transaction => transaction.OldestTransaction).Where(value => value.HasValue).Min()),
            [new ActivityPoint(snapshot.CapturedAt, activeStatements)]);
    }

    public DashboardSnapshot ProjectError(string message, DateTimeOffset now)
    {
        return new DashboardSnapshot(
            DatabaseHealthStatus.Critical,
            message,
            now,
            CreateMetrics(0, 0, 0, null),
            []);
    }

    private static DatabaseHealthStatus ResolveHealth(MonitoringSnapshot snapshot, DateTimeOffset now)
    {
        if (now - snapshot.CapturedAt > TimeSpan.FromSeconds(10))
        {
            return DatabaseHealthStatus.Stale;
        }

        return snapshot.Transactions.Any(transaction => transaction.StartedAt is null)
            ? DatabaseHealthStatus.Warning
            : DatabaseHealthStatus.Healthy;
    }

    private static IReadOnlyList<DashboardMetric> CreateMetrics(
        int attachments,
        int transactions,
        int statements,
        long? oldestTransaction)
    {
        return
        [
            new("health", "Health", attachments > 0 ? "Online" : "Sem conexão", "Estado operacional"),
            new("attachments", "Attachments", attachments.ToString(System.Globalization.CultureInfo.InvariantCulture), "Conexões observadas"),
            new("transactions", "Transações", transactions.ToString(System.Globalization.CultureInfo.InvariantCulture), "Transações MON$"),
            new("statements", "Statements", statements.ToString(System.Globalization.CultureInfo.InvariantCulture), "Statements ativos"),
            new("oldest", "OIT", oldestTransaction?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "-", "Oldest transaction")
        ];
    }
}

using FirebirdAdmin.Application.History;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FirebirdAdmin.Infrastructure.History;

public sealed class HistoryRetentionHostedService(
    IRetentionPolicyService retentionPolicyService,
    ILogger<HistoryRetentionHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await retentionPolicyService.ApplyRetentionAsync(stoppingToken);
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("Serviço de retenção do histórico encerrado.");
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha ao aplicar a política de retenção do histórico.");
            }
        }
    }
}

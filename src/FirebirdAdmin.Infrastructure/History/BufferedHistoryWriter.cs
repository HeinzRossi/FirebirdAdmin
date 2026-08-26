using System.Threading.Channels;
using FirebirdAdmin.Application.History;
using FirebirdAdmin.Application.Monitoring;
using FirebirdAdmin.Application.Profiler;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FirebirdAdmin.Infrastructure.History;

public sealed class BufferedHistoryWriter(
    DapperHistoryWriter innerWriter,
    ILogger<BufferedHistoryWriter> logger) : IHistoryWriter, IHostedService
{
    private const int MaximumBatchSize = 250;
    private static readonly TimeSpan FlushInterval = TimeSpan.FromMilliseconds(250);

    private readonly Channel<HistoryWriteRequest> channel = Channel.CreateBounded<HistoryWriteRequest>(
        new BoundedChannelOptions(2048)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });

    private CancellationTokenSource? workerCts;
    private Task? workerTask;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        workerCts = new CancellationTokenSource();
        workerTask = ProcessAsync(workerCts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        channel.Writer.TryComplete();
        if (workerTask is not null)
        {
            await workerTask.WaitAsync(cancellationToken);
        }

        workerCts?.Dispose();
        workerCts = null;
    }

    public async Task WriteProfilerEventsAsync(
        Guid? connectionProfileId,
        IReadOnlyList<ProfilerEvent> events,
        CancellationToken cancellationToken)
    {
        if (events.Count > 0)
        {
            await channel.Writer.WriteAsync(
                new HistoryWriteRequest(connectionProfileId, events, []),
                cancellationToken);
        }
    }

    public async Task WriteMonitoringSnapshotsAsync(
        Guid? connectionProfileId,
        IReadOnlyList<MonitoringSnapshot> snapshots,
        CancellationToken cancellationToken)
    {
        if (snapshots.Count > 0)
        {
            await channel.Writer.WriteAsync(
                new HistoryWriteRequest(connectionProfileId, [], snapshots),
                cancellationToken);
        }
    }

    private async Task ProcessAsync(CancellationToken cancellationToken)
    {
        var batch = new List<HistoryWriteRequest>(MaximumBatchSize);

        try
        {
            while (await channel.Reader.WaitToReadAsync(cancellationToken))
            {
                DrainAvailable(batch);
                if (batch.Count < MaximumBatchSize)
                {
                    await Task.Delay(FlushInterval, cancellationToken);
                    DrainAvailable(batch);
                }

                await FlushAsync(batch, cancellationToken);
                batch.Clear();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            while (channel.Reader.TryRead(out var request))
            {
                batch.Add(request);
            }

            if (batch.Count > 0)
            {
                await FlushAsync(batch, CancellationToken.None);
            }
        }
    }

    private void DrainAvailable(List<HistoryWriteRequest> batch)
    {
        while (batch.Count < MaximumBatchSize && channel.Reader.TryRead(out var request))
        {
            batch.Add(request);
        }
    }

    private async Task FlushAsync(IReadOnlyList<HistoryWriteRequest> batch, CancellationToken cancellationToken)
    {
        foreach (var group in batch.GroupBy(request => request.ConnectionProfileId))
        {
            var profilerEvents = group.SelectMany(request => request.ProfilerEvents).ToArray();
            var monitoringSnapshots = group.SelectMany(request => request.MonitoringSnapshots).ToArray();

            await PersistWithRetryAsync(
                token => innerWriter.WriteProfilerEventsAsync(group.Key, profilerEvents, token),
                profilerEvents.Length,
                cancellationToken);
            await PersistWithRetryAsync(
                token => innerWriter.WriteMonitoringSnapshotsAsync(group.Key, monitoringSnapshots, token),
                monitoringSnapshots.Length,
                cancellationToken);
        }
    }

    private async Task PersistWithRetryAsync(
        Func<CancellationToken, Task> persist,
        int itemCount,
        CancellationToken cancellationToken)
    {
        if (itemCount == 0)
        {
            return;
        }

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                await persist(cancellationToken);
                return;
            }
            catch (Exception ex) when (attempt < 3 && !cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Falha transitória ao persistir histórico; nova tentativa {Attempt}/3.", attempt + 1);
                await Task.Delay(TimeSpan.FromMilliseconds(100 * attempt), cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha ao persistir {Count} item(ns) do histórico.", itemCount);
                return;
            }
        }
    }

    private sealed record HistoryWriteRequest(
        Guid? ConnectionProfileId,
        IReadOnlyList<ProfilerEvent> ProfilerEvents,
        IReadOnlyList<MonitoringSnapshot> MonitoringSnapshots);
}

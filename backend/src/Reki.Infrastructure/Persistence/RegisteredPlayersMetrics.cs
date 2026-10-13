using System.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Reki.Application.Common;

namespace Reki.Infrastructure.Persistence;

internal sealed partial class RegisteredPlayersMetrics(
    IGameStore store,
    IMeterFactory meters,
    ILogger<RegisteredPlayersMetrics> logger,
    TimeProvider timeProvider) : BackgroundService
{
    private long _count = -1;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var meter = meters.Create("Reki.Product");
        meter.CreateObservableGauge("reki.players.registered", ObserveCount, "{player}", "Current registered profiles in MongoDB; no player identifiers.");
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        do
        {
            try
            {
                await RefreshSnapshotAsync(stoppingToken);
            }
            catch (MongoException)
            {
                Interlocked.Exchange(ref _count, -1);
                LogRefreshFailed();
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RefreshSnapshotAsync(CancellationToken stoppingToken)
    {
        var snapshot = timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
        long count = 0;
        await foreach (var playerId in store.GetPlayerIdsAsync(stoppingToken))
        {
            using var scope = logger.BeginScope(new Dictionary<string, object?>
            {
                ["reki.stream"] = "product",
                ["activity_kind"] = "inventory",
                ["event_name"] = "profile_snapshot",
                ["snapshot_time_unix_ms"] = snapshot,
                ["player_id"] = playerId.Value,
            });
            LogProfileSnapshot();
            count++;
        }

        using (logger.BeginScope(new Dictionary<string, object?>
        {
            ["reki.stream"] = "product",
            ["activity_kind"] = "inventory",
            ["event_name"] = "profiles_snapshot_complete",
            ["snapshot_time_unix_ms"] = snapshot,
            ["registered_count"] = count,
        }))
        {
            LogSnapshotComplete();
        }

        Interlocked.Exchange(ref _count, count);
    }

    private IEnumerable<Measurement<long>> ObserveCount()
    {
        var count = Interlocked.Read(ref _count);
        return count < 0 ? [] : [new Measurement<long>(count)];
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Profile inventory snapshot")]
    private partial void LogProfileSnapshot();

    [LoggerMessage(Level = LogLevel.Information, Message = "Profile inventory snapshot complete")]
    private partial void LogSnapshotComplete();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Unable to refresh registered-profile count")]
    private partial void LogRefreshFailed();
}

using System.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Reki.Domain.Player;

namespace Reki.Infrastructure.Persistence;

internal sealed partial class RegisteredPlayersMetrics(
    MongoCompositionRoot compositionRoot,
    IMeterFactory meters,
    ILogger<RegisteredPlayersMetrics> logger,
    TimeProvider timeProvider) : BackgroundService
{
    private readonly IMongoCollection<PlayerProfile> _players = compositionRoot.MongoDatabase.GetCollection<PlayerProfile>(MongoCollections.Players);
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
        var profiles = await _players.Find(FilterDefinition<PlayerProfile>.Empty).ToListAsync(stoppingToken);
        var snapshot = timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
        foreach (var player in profiles)
        {
            using var scope = logger.BeginScope(new Dictionary<string, object?>
            {
                ["reki.stream"] = "product",
                ["activity_kind"] = "inventory",
                ["event_name"] = "profile_snapshot",
                ["snapshot_time_unix_ms"] = snapshot,
                ["player_id"] = player.Id.Value,
                ["world"] = player.World.Value,
                ["age_group"] = player.AgeBand ?? player.BirthGroup,
                ["registration_source"] = player.InvitedBy is null ? "direct" : "referral",
                ["launch_surface"] = "unknown",
                ["subscriber_total"] = player.Subscribers,
            });
            LogProfileSnapshot();
        }

        using (logger.BeginScope(new Dictionary<string, object?>
        {
            ["reki.stream"] = "product",
            ["activity_kind"] = "inventory",
            ["event_name"] = "profiles_snapshot_complete",
            ["snapshot_time_unix_ms"] = snapshot,
            ["registered_count"] = profiles.Count,
        }))
        {
            LogSnapshotComplete();
        }

        Interlocked.Exchange(ref _count, profiles.Count);
    }

    private IEnumerable<Measurement<long>> ObserveCount()
    {
        var count = Interlocked.Read(ref _count);
        return count < 0 ? [] : [new Measurement<long>(count)];
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Profile inventory snapshot.")]
    private partial void LogProfileSnapshot();

    [LoggerMessage(Level = LogLevel.Information, Message = "Profile inventory snapshot complete.")]
    private partial void LogSnapshotComplete();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Unable to refresh registered-profile count.")]
    private partial void LogRefreshFailed();
}

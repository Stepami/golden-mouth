using System.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Reki.Domain.Player;

namespace Reki.Infrastructure.Persistence;

internal sealed partial class RegisteredPlayersMetrics : BackgroundService
{
    private readonly IMongoCollection<PlayerProfile> _players;
    private readonly ILogger<RegisteredPlayersMetrics> _logger;
    private long _count = -1;

    public RegisteredPlayersMetrics(MongoCompositionRoot compositionRoot, IMeterFactory meters, ILogger<RegisteredPlayersMetrics> logger)
    {
        _players = compositionRoot.MongoDatabase.GetCollection<PlayerProfile>(MongoCollections.Players);
        _logger = logger;
        var meter = meters.Create("Reki.Product");
        meter.CreateObservableGauge("reki.players.registered", ObserveCount, "{player}", "Current registered profiles in MongoDB; no player identifiers.");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        do
        {
            try
            {
                var count = await _players.CountDocumentsAsync(FilterDefinition<PlayerProfile>.Empty, cancellationToken: stoppingToken);
                Interlocked.Exchange(ref _count, count);
            }
            catch (MongoException)
            {
                Interlocked.Exchange(ref _count, -1);
                LogRefreshFailed();
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private IEnumerable<Measurement<long>> ObserveCount()
    {
        var count = Interlocked.Read(ref _count);
        return count < 0 ? [] : [new Measurement<long>(count)];
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Unable to refresh registered-profile count.")]
    private partial void LogRefreshFailed();
}

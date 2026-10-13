using System.Diagnostics.Metrics;
using MongoDB.Driver;
using Reki.Domain.Player;
using Reki.Infrastructure.Persistence;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Interfaces;

namespace Reki.Infrastructure.Jobs;

public sealed class RefreshRegisteredPlayersJob : ITickerFunction
{
    private readonly IMongoCollection<PlayerProfile> _players;
    private long _count = -1;

    public RefreshRegisteredPlayersJob(IMongoDatabase database, IMeterFactory meters)
    {
        _players = database.GetCollection<PlayerProfile>(MongoCollections.Players);
        var meter = meters.Create("Reki.Product");
        meter.CreateObservableGauge("reki.players.registered", ObserveCount, "{player}", "Current registered profiles in MongoDB; no player identifiers.");
    }

    public async Task ExecuteAsync(TickerFunctionContext context, CancellationToken cancellationToken = default)
    {
        Interlocked.Exchange(ref _count, -1);
        var count = await _players.CountDocumentsAsync(FilterDefinition<PlayerProfile>.Empty, cancellationToken: cancellationToken);
        Interlocked.Exchange(ref _count, count);
    }

    private IEnumerable<Measurement<long>> ObserveCount()
    {
        var count = Interlocked.Read(ref _count);
        return count < 0 ? [] : [new Measurement<long>(count)];
    }
}

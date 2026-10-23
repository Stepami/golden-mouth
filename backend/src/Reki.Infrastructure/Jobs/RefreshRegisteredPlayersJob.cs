using System.Diagnostics.Metrics;
using Reki.Application.Player;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Interfaces;

namespace Reki.Infrastructure.Jobs;

internal sealed class RefreshRegisteredPlayersJob : ITickerFunction
{
    private readonly IPlayerRepository _players;
    private long _count = -1;

    public RefreshRegisteredPlayersJob(IPlayerRepository players, IMeterFactory meters)
    {
        _players = players;
        var meter = meters.Create("Reki.Product");
        meter.CreateObservableGauge("reki.players.registered", ObserveCount, "{player}", "Current registered profiles in MongoDB; no player identifiers.");
    }

    public async Task ExecuteAsync(TickerFunctionContext context, CancellationToken cancellationToken = default)
    {
        Interlocked.Exchange(ref _count, -1);
        var count = await _players.CountPlayersAsync(cancellationToken);
        Interlocked.Exchange(ref _count, count);
    }

    private IEnumerable<Measurement<long>> ObserveCount()
    {
        var count = Interlocked.Read(ref _count);
        return count < 0 ? [] : [new Measurement<long>(count)];
    }
}

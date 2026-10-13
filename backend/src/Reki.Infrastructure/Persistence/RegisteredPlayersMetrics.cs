using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Reki.Application.Common;

namespace Reki.Infrastructure.Persistence;

public sealed partial class RegisteredPlayersMetrics
{
    private readonly IGameStore _store;
    private readonly ILogger<RegisteredPlayersMetrics> _logger;
    private long _count = -1;

    public RegisteredPlayersMetrics(IGameStore store, IMeterFactory meters, ILogger<RegisteredPlayersMetrics> logger)
    {
        _store = store;
        _logger = logger;
        var meter = meters.Create("Reki.Product");
        meter.CreateObservableGauge("reki.players.registered", ObserveCount, "{player}", "Current registered profiles in MongoDB; no player identifiers.");
    }

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        try
        {
            var count = await _store.CountPlayersAsync(cancellationToken);
            Interlocked.Exchange(ref _count, count);
        }
        catch (MongoException)
        {
            Interlocked.Exchange(ref _count, -1);
            LogRefreshFailed();
            throw;
        }
    }

    private IEnumerable<Measurement<long>> ObserveCount()
    {
        var count = Interlocked.Read(ref _count);
        return count < 0 ? [] : [new Measurement<long>(count)];
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Unable to refresh registered-profile count")]
    private partial void LogRefreshFailed();
}

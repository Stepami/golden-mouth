using System.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Reki.Application.Common;

namespace Reki.Infrastructure.Persistence;

internal sealed partial class RegisteredPlayersMetrics(
    IGameStore store,
    IMeterFactory meters,
    ILogger<RegisteredPlayersMetrics> logger) : BackgroundService
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
                await RefreshCountAsync(stoppingToken);
            }
            catch (MongoException)
            {
                Interlocked.Exchange(ref _count, -1);
                LogRefreshFailed();
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RefreshCountAsync(CancellationToken stoppingToken)
    {
        var count = await store.CountPlayersAsync(stoppingToken);
        Interlocked.Exchange(ref _count, count);
    }

    private IEnumerable<Measurement<long>> ObserveCount()
    {
        var count = Interlocked.Read(ref _count);
        return count < 0 ? [] : [new Measurement<long>(count)];
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Unable to refresh registered-profile count")]
    private partial void LogRefreshFailed();
}

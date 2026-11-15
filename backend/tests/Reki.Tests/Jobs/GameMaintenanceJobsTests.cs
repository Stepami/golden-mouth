using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Domain.Player;
using Reki.Infrastructure.Jobs;
using Reki.Tests.Api;

namespace Reki.Tests.Jobs;

[ClassDataSource<ApiFactory>(Shared = SharedType.PerTestSession)]
[ParallelGroup("Api", Order = 1)]
[NotInParallel("ApiState")]
public sealed class GameMaintenanceJobsTests(ApiFactory factory) : GameApiTestBase(factory)
{
    [Test]
    public async Task RefreshCount_AccountChanges_ExportsMongoCountAsync()
    {
        using var http = Factory.CreateClient();
        var expectedMeter = Factory.Services.GetRequiredService<IMeterFactory>().Create("Reki.Product");
        using var listener = new MeterListener();
        long? count = null;
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Name == "reki.players.registered" && ReferenceEquals(instrument.Meter, expectedMeter))
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, measurement, _, _) => count = measurement);
        listener.Start();
        await using var scope = Factory.Services.CreateAsyncScope();
        var job = scope.ServiceProvider.GetRequiredService<RefreshRegisteredPlayersJob>();
        await job.ExecuteAsync(new(), CancellationToken.None);
        listener.RecordObservableInstruments();
        await Assert.That(count).IsEqualTo(0L);
        using var firstApi = Factory.CreateApiClient(9305);
        using var secondApi = Factory.CreateApiClient(9306);
        await CreatePlayerWithWorldAsync(firstApi);
        await CreatePlayerWithWorldAsync(secondApi);

        await job.ExecuteAsync(new(), CancellationToken.None);
        listener.RecordObservableInstruments();
        await Assert.That(count).IsEqualTo(2L);
        await firstApi.Player.DeleteCurrentPlayerAsync(CancellationToken.None);
        await job.ExecuteAsync(new(), CancellationToken.None);
        listener.RecordObservableInstruments();

        await Assert.That(count).IsEqualTo(1L);
        await Assert.That(await Database.GetCollection<PlayerProfile>("players").CountDocumentsAsync(FilterDefinition<PlayerProfile>.Empty, cancellationToken: CancellationToken.None)).IsEqualTo(1L);
    }

    [Test]
    public async Task Maintenance_CanceledExecution_CancellationAsync()
    {
        using var api = Factory.CreateApiClient(9307);
        await CreatePlayerWithWorldAsync(api);
        var beforePlayer = await Database.GetCollection<BsonDocument>("players").Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
        var beforeWorld = await Database.GetCollection<BsonDocument>("worlds").Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
        Factory.Session.Clock.SetUtcNow(Factory.Session.Clock.GetUtcNow().AddDays(1));
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        await using var scope = Factory.Services.CreateAsyncScope();
        var countJob = scope.ServiceProvider.GetRequiredService<RefreshRegisteredPlayersJob>();

        await Assert.That(() => countJob.ExecuteAsync(new(), cancellation.Token))
            .Throws<OperationCanceledException>();

        await Assert.That(await Database.GetCollection<BsonDocument>("players").Find(FilterDefinition<BsonDocument>.Empty).SingleAsync()).IsEqualTo(beforePlayer);
        await Assert.That(await Database.GetCollection<BsonDocument>("worlds").Find(FilterDefinition<BsonDocument>.Empty).SingleAsync()).IsEqualTo(beforeWorld);
    }
}

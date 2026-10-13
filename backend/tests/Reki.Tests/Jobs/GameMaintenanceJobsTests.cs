using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Application.Common;
using Reki.Domain.Player;
using Reki.Domain.Worlds;
using Reki.Infrastructure.Jobs;
using Reki.Tests.Api;

namespace Reki.Tests.Jobs;

[ClassDataSource<ApiFactory>(Shared = SharedType.PerTestSession)]
[ParallelGroup("Api", Order = 1)]
[NotInParallel("ApiState")]
public sealed class GameMaintenanceJobsTests(ApiFactory factory) : GameApiTestBase(factory)
{
    [Test]
    [Arguments(-1)]
    [Arguments(0)]
    [Arguments(1)]
    [Arguments(3 * 24 * 60 * 60)]
    public async Task Reset_ScheduledBoundary_UpdatesStaleDocumentsOnceAsync(int secondsAfterReset)
    {
        var reset = new DateTimeOffset(2026, 10, 6, 6, 0, 0, TimeSpan.Zero);
        using var api = Factory.CreateApiClient(9301);
        var response = await CreatePlayerWithWorldAsync(api);
        var players = Database.GetCollection<PlayerProfile>("players");
        var worlds = Database.GetCollection<World>("worlds");
        var player = await players.Find(item => item.Id == PlayerId.From(response.Id)).SingleAsync();
        var world = await worlds.Find(item => item.Player.PlayerId == player.Id).SingleAsync();
        var seededPlayer = player with { EnergyUsed = 7, Energy = 8, PlusInterestClaimed = true };
        var seededWorld = world with
        {
            Stars = world.Stars.Select(star => star with { Relation = 4, RelationGainedInPeriod = 3 }).ToArray(),
            Player = world.Player with
            {
                Subscribers = 1234,
                Wants = world.Player.Wants.Select(want => want with { Done = true }).ToArray(),
                LastPostAtUtc = reset.AddDays(-3),
                RestedPostsRemaining = 1,
            },
        };
        await players.ReplaceOneAsync(item => item.Id == player.Id, seededPlayer);
        await worlds.ReplaceOneAsync(item => item.Id == world.Id, seededWorld);
        var otherPlayer = seededPlayer with { Id = PlayerId.New(), TelegramUserId = TelegramUserId.From(9391) };
        var otherWorld = seededWorld with { Id = WorldId.New(), Player = seededWorld.Player with { PlayerId = otherPlayer.Id } };
        await players.InsertOneAsync(otherPlayer);
        await worlds.InsertOneAsync(otherWorld);
        Factory.Session.Clock.SetUtcNow(reset.AddSeconds(secondsAfterReset));

        await ResetGameDayAsync();

        var saved = await players.Find(item => item.Id == player.Id).SingleAsync();
        var savedWorld = await worlds.Find(item => item.Id == world.Id).SingleAsync();
        var shouldReset = secondsAfterReset >= 0;
        var period = shouldReset ? reset.AddDays(Math.Floor((double)secondsAfterReset / (24 * 60 * 60))) : player.PostsPeriodStartedAtUtc;
        await Assert.That(saved.EnergyUsed).IsEqualTo(shouldReset ? 0 : 7);
        await Assert.That(saved.Energy).IsEqualTo(shouldReset ? 10 : 8);
        await Assert.That(saved.Version).IsEqualTo(player.Version + (shouldReset ? 1 : 0));
        await Assert.That(saved.PostsPeriodStartedAtUtc).IsEqualTo(period);
        await Assert.That(saved.PlusInterestClaimed).IsTrue();
        var savedOther = await players.Find(item => item.Id == otherPlayer.Id).SingleAsync();
        var savedOtherWorld = await worlds.Find(item => item.Id == otherWorld.Id).SingleAsync();
        await Assert.That(savedOther.EnergyUsed).IsEqualTo(shouldReset ? 0 : 7);
        await Assert.That(savedOther.Version).IsEqualTo(otherPlayer.Version + (shouldReset ? 1 : 0));
        await Assert.That(savedOtherWorld.Version).IsEqualTo(otherWorld.Version + (shouldReset ? 1 : 0));
        await Assert.That(savedOtherWorld.Player.PeriodStartedAtUtc).IsEqualTo(period);
        await Assert.That(savedWorld.Version).IsEqualTo(world.Version + (shouldReset ? 1 : 0));
        await Assert.That(savedWorld.Player.PeriodStartedAtUtc).IsEqualTo(period);
        await Assert.That(savedWorld.Player.Subscribers).IsEqualTo(1234);
        await Assert.That(savedWorld.Player.LastPostAtUtc).IsEqualTo(reset.AddDays(-3));
        await Assert.That(savedWorld.Player.RestedPostsRemaining).IsEqualTo(shouldReset ? 3 : 1);
        await Assert.That(savedWorld.Player.Wants.Count).IsEqualTo(seededWorld.Player.Wants.Count);
        await Assert.That(savedWorld.Stars.Count).IsEqualTo(seededWorld.Stars.Count);
        await Assert.That(savedWorld.Player.Wants.All(want => want.Done == !shouldReset)).IsTrue();
        await Assert.That(savedWorld.Stars.All(star => star.Relation == 4 &&
            star.RelationGainedInPeriod == (shouldReset ? 0 : 3) && star.RelationPeriodStartedAtUtc == period)).IsTrue();
        var playerBson = await Database.GetCollection<BsonDocument>("players").Find(new BsonDocument("_id", saved.Id.Value)).SingleAsync();
        var worldBson = await Database.GetCollection<BsonDocument>("worlds").Find(new BsonDocument("_id", savedWorld.Id.Value)).SingleAsync();

        await ResetGameDayAsync();

        await Assert.That(await Database.GetCollection<BsonDocument>("players").Find(new BsonDocument("_id", saved.Id.Value)).SingleAsync()).IsEqualTo(playerBson);
        await Assert.That(await Database.GetCollection<BsonDocument>("worlds").Find(new BsonDocument("_id", savedWorld.Id.Value)).SingleAsync()).IsEqualTo(worldBson);
    }

    [Test]
    [Arguments(0)]
    [Arguments(1)]
    public async Task Reset_NonStalePeriods_PreservesDocumentsAsync(int daysAhead)
    {
        using var api = Factory.CreateApiClient(9302);
        var response = await CreatePlayerWithWorldAsync(api);
        var players = Database.GetCollection<PlayerProfile>("players");
        var worlds = Database.GetCollection<World>("worlds");
        var player = await players.Find(item => item.Id == PlayerId.From(response.Id)).SingleAsync();
        var world = await worlds.Find(item => item.Player.PlayerId == player.Id).SingleAsync();
        var period = player.PostsPeriodStartedAtUtc.AddDays(daysAhead);
        await players.ReplaceOneAsync(item => item.Id == player.Id, player with { PostsPeriodStartedAtUtc = period, EnergyUsed = 7, Energy = 8 });
        await worlds.ReplaceOneAsync(item => item.Id == world.Id, world with { Player = world.Player with { PeriodStartedAtUtc = period, RestedPostsRemaining = 2 } });
        var beforePlayer = await Database.GetCollection<BsonDocument>("players").Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
        var beforeWorld = await Database.GetCollection<BsonDocument>("worlds").Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();

        await ResetGameDayAsync();

        await Assert.That(await Database.GetCollection<BsonDocument>("players").Find(FilterDefinition<BsonDocument>.Empty).SingleAsync()).IsEqualTo(beforePlayer);
        await Assert.That(await Database.GetCollection<BsonDocument>("worlds").Find(FilterDefinition<BsonDocument>.Empty).SingleAsync()).IsEqualTo(beforeWorld);
    }

    [Test]
    [Arguments(-1, 0)]
    [Arguments(1, 0)]
    [Arguments(2, 0)]
    [Arguments(3, 3)]
    public async Task Reset_RestBonus_UsesSkippedDaysAsync(int lastPostDaysAgo, int expectedBonus)
    {
        using var api = Factory.CreateApiClient(9303);
        var response = await CreatePlayerWithWorldAsync(api);
        var worlds = Database.GetCollection<World>("worlds");
        var world = await worlds.Find(item => item.Player.PlayerId == PlayerId.From(response.Id)).SingleAsync();
        var reset = world.Player.PeriodStartedAtUtc.AddDays(1);
        await worlds.ReplaceOneAsync(item => item.Id == world.Id, world with
        {
            Stars = [],
            Player = world.Player with { Wants = [], LastPostAtUtc = lastPostDaysAgo < 0 ? null : reset.AddDays(-lastPostDaysAgo), RestedPostsRemaining = 7 },
        });
        Factory.Session.Clock.SetUtcNow(reset);

        await ResetGameDayAsync();

        var saved = await worlds.Find(item => item.Id == world.Id).SingleAsync();
        await Assert.That(saved.Player.RestedPostsRemaining).IsEqualTo(expectedBonus);
        await Assert.That(saved.Stars.Count).IsEqualTo(0);
        await Assert.That(saved.Player.Wants.Count).IsEqualTo(0);
        await Assert.That(saved.Version).IsEqualTo(world.Version + 1);
    }

    [Test]
    public async Task GetPlayerWorlds_StaleDay_DoesNotWriteResetAsync()
    {
        using var api = Factory.CreateApiClient(9304);
        var response = await CreatePlayerWithWorldAsync(api);
        var players = Database.GetCollection<PlayerProfile>("players");
        var player = await players.Find(item => item.Id == PlayerId.From(response.Id)).SingleAsync();
        await players.ReplaceOneAsync(item => item.Id == player.Id, player with { EnergyUsed = 7, Energy = 8 });
        var beforePlayer = await Database.GetCollection<BsonDocument>("players").Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
        var beforeWorld = await Database.GetCollection<BsonDocument>("worlds").Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
        Factory.Session.Clock.SetUtcNow(player.PostsPeriodStartedAtUtc.AddDays(1));

        var current = await api.Player.GetCurrentPlayerAsync(CancellationToken.None);
        var owned = await api.Worlds.GetOwnedWorldsAsync(CancellationToken.None);

        await Assert.That(current.Energy).IsEqualTo(8);
        await Assert.That(owned.Count).IsEqualTo(1);
        await Assert.That(await Database.GetCollection<BsonDocument>("players").Find(FilterDefinition<BsonDocument>.Empty).SingleAsync()).IsEqualTo(beforePlayer);
        await Assert.That(await Database.GetCollection<BsonDocument>("worlds").Find(FilterDefinition<BsonDocument>.Empty).SingleAsync()).IsEqualTo(beforeWorld);
    }

    [Test]
    [Arguments(false, -1, 10)]
    [Arguments(false, 0, 10)]
    [Arguments(false, 1, 13)]
    [Arguments(true, -1, 20)]
    [Arguments(true, 0, 20)]
    [Arguments(true, 1, 23)]
    public async Task Reset_ChallengeExpiryBoundary_SetsCurrentBalanceEnergyAsync(bool plus, int expirySeconds, int expectedEnergy)
    {
        // Arrange
        using var api = Factory.CreateApiClient(9308);
        var response = await CreatePlayerWithWorldAsync(api);
        var players = Database.GetCollection<PlayerProfile>("players");
        var player = await players.Find(item => item.Id == PlayerId.From(response.Id)).SingleAsync();
        var reset = player.PostsPeriodStartedAtUtc.AddDays(1);
        await players.ReplaceOneAsync(item => item.Id == player.Id, player with
        {
            Energy = 31,
            EnergyUsed = 4,
            Settings = player.Settings with { Plus = plus },
            ChallengeBonusUntilUtc = reset.AddSeconds(expirySeconds),
        });
        Factory.Session.Clock.SetUtcNow(reset);

        // Act
        await ResetGameDayAsync();

        // Assert
        var saved = await players.Find(item => item.Id == player.Id).SingleAsync();
        await Assert.That(saved.Energy).IsEqualTo(expectedEnergy);
        await Assert.That(saved.EnergyUsed).IsEqualTo(0);
        await Assert.That(saved.ChallengeBonusUntilUtc).IsEqualTo(reset.AddSeconds(expirySeconds));
        await Assert.That(saved.PostsPeriodStartedAtUtc).IsEqualTo(reset);
        await Assert.That(saved.Version).IsEqualTo(player.Version + 1);
        await ResetGameDayAsync();
        await Assert.That((await players.Find(item => item.Id == player.Id).SingleAsync()).Energy).IsEqualTo(expectedEnergy);
    }

    [Test]
    public async Task ChallengeReward_ActiveRenewal_DoesNotStackEnergyAsync()
    {
        // Arrange
        using var api = Factory.CreateApiClient(9309);
        var response = await CreatePlayerWithWorldAsync(api);
        var players = Database.GetCollection<PlayerProfile>("players");
        var player = await players.Find(item => item.Id == PlayerId.From(response.Id)).SingleAsync();
        var period = player.PostsPeriodStartedAtUtc;
        var spent = player with { Energy = 6, EnergyUsed = 4 };

        // Act
        var first = spent.ApplyChallengeWinBonus(3, period.AddDays(3), period);
        var renewed = first.ApplyChallengeWinBonus(3, period.AddDays(4), period);
        await players.ReplaceOneAsync(item => item.Id == player.Id, renewed);

        // Assert
        await Assert.That(first.Energy).IsEqualTo(9);
        await Assert.That(renewed.Energy).IsEqualTo(9);
        await Assert.That(renewed.EnergyUsed).IsEqualTo(4);
        await Assert.That(renewed.ChallengeBonusUntilUtc).IsEqualTo(period.AddDays(4));
        await Assert.That((await api.Player.GetCurrentPlayerAsync(CancellationToken.None)).Energy).IsEqualTo(9);
        Factory.Session.Clock.SetUtcNow(period.AddDays(1));
        await ResetGameDayAsync();
        var reset = await players.Find(item => item.Id == player.Id).SingleAsync();
        await Assert.That(reset.Energy).IsEqualTo(13);
        await Assert.That(reset.EnergyUsed).IsEqualTo(0);
    }

    [Test]
    public async Task Reset_ChangedBalance_AppliesOnlyAtNextPeriodAsync()
    {
        // Arrange
        using var api = Factory.CreateApiClient(9310);
        var response = await CreatePlayerWithWorldAsync(api);
        var players = Database.GetCollection<PlayerProfile>("players");
        var player = await players.Find(item => item.Id == PlayerId.From(response.Id)).SingleAsync();
        await players.ReplaceOneAsync(item => item.Id == player.Id, player with { Energy = 6, EnergyUsed = 4 });
        var changed = Options.Create(Factory.Services.GetRequiredService<IOptions<GameBalance>>().Value with { FreePostsPerDay = 12 });
        var job = new ResetGameDayJob(Database, new GameDayPeriod(Factory.Session.Clock, changed), changed, NullLogger<ResetGameDayJob>.Instance);

        // Act
        await job.ExecuteAsync(new(), CancellationToken.None);
        var beforeReset = await api.Player.GetCurrentPlayerAsync(CancellationToken.None);
        Factory.Session.Clock.SetUtcNow(player.PostsPeriodStartedAtUtc.AddDays(1));
        await job.ExecuteAsync(new(), CancellationToken.None);
        var afterReset = await api.Player.GetCurrentPlayerAsync(CancellationToken.None);

        // Assert
        await Assert.That(beforeReset.Energy).IsEqualTo(6);
        await Assert.That(afterReset.Energy).IsEqualTo(12);
        var stored = await players.Find(item => item.Id == player.Id).SingleAsync();
        await Assert.That(stored.EnergyUsed).IsEqualTo(0);
        await Assert.That(stored.Version).IsEqualTo(player.Version + 1);
    }

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
    public async Task Dashboard_CronEndpoint_RequiresCorrectBasicCredentialsAsync()
    {
        using var http = Factory.CreateClient();
        var endpoint = Factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>().Single(item => item.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == "GetCronTickers");
        var path = "/admin/tickerq" + (endpoint.RoutePattern.RawText ?? throw new InvalidOperationException("Missing dashboard route."));
        using var anonymous = await http.GetAsync(path, CancellationToken.None);
        await Assert.That(anonymous.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes("operator:wrong")));
        using var rejected = await http.GetAsync(path, CancellationToken.None);
        await Assert.That(rejected.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes("operator:placeholder")));

        using var accepted = await http.GetAsync(path, CancellationToken.None);

        await Assert.That(accepted.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var body = await accepted.Content.ReadAsStringAsync(CancellationToken.None);
        await Assert.That(body).Contains(nameof(ResetGameDayJob));
        await Assert.That(body).Contains(nameof(RefreshRegisteredPlayersJob));
    }

    [Test]
    public async Task Startup_PersistsTwoRecurringSchedulesInMongoAsync()
    {
        using var http = Factory.CreateClient();

        var schedules = await Database.GetCollection<BsonDocument>("ticker_CronTickers")
            .Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();

        await Assert.That(schedules.Count).IsEqualTo(2);
        var reset = schedules.Single(item => item["Function"].AsString == nameof(ResetGameDayJob));
        var count = schedules.Single(item => item["Function"].AsString == nameof(RefreshRegisteredPlayersJob));
        await Assert.That(reset["Expression"].AsString).IsEqualTo("0 0 9 * * *");
        await Assert.That(count["Expression"].AsString).IsEqualTo("0 * * * * *");
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Maintenance_CanceledExecution_CancellationAsync(bool refreshCount)
    {
        using var api = Factory.CreateApiClient(9307);
        await CreatePlayerWithWorldAsync(api);
        var beforePlayer = await Database.GetCollection<BsonDocument>("players").Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
        var beforeWorld = await Database.GetCollection<BsonDocument>("worlds").Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
        Factory.Session.Clock.SetUtcNow(Factory.Session.Clock.GetUtcNow().AddDays(1));
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        await using var scope = Factory.Services.CreateAsyncScope();
        var resetJob = scope.ServiceProvider.GetRequiredService<ResetGameDayJob>();
        var countJob = scope.ServiceProvider.GetRequiredService<RefreshRegisteredPlayersJob>();

        await Assert.That(() => refreshCount ? countJob.ExecuteAsync(new(), cancellation.Token) : resetJob.ExecuteAsync(new(), cancellation.Token))
            .Throws<OperationCanceledException>();

        await Assert.That(await Database.GetCollection<BsonDocument>("players").Find(FilterDefinition<BsonDocument>.Empty).SingleAsync()).IsEqualTo(beforePlayer);
        await Assert.That(await Database.GetCollection<BsonDocument>("worlds").Find(FilterDefinition<BsonDocument>.Empty).SingleAsync()).IsEqualTo(beforeWorld);
    }

    [Test]
    public async Task Dashboard_TelegramAuthentication_HandlesBasicCredentialsAsync()
    {
        using var productionAuthFactory = new ApiFactory
        {
            Session = Factory.Session,
            UseProductionAuthentication = true,
        };
        using var http = productionAuthFactory.CreateClient();
        const string path = "/admin/tickerq/api/cron-tickers";
        using var anonymous = await http.GetAsync(path, CancellationToken.None);
        await Assert.That(anonymous.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes("operator:wrong")));
        using var rejected = await http.GetAsync(path, CancellationToken.None);
        await Assert.That(rejected.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes("operator:placeholder")));

        using var accepted = await http.GetAsync(path, CancellationToken.None);

        await Assert.That(accepted.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(await accepted.Content.ReadAsStringAsync(CancellationToken.None)).Contains(nameof(ResetGameDayJob));
    }
}

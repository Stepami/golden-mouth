using MongoDB.Bson;
using MongoDB.Driver;
using NSubstitute;
using Reki.Contracts.Telemetry;

namespace Reki.Tests.Api;

[ClassDataSource<ApiFactory>(Shared = SharedType.PerTestSession)]
[ParallelGroup("Api", Order = 1)]
[NotInParallel("ApiState")]
public sealed class TelemetryControllerTests(ApiFactory factory) : GameApiTestBase(factory)
{
    [Test]
    public async Task WorldCreated_ClientSpoof_LeavesSingleServerEventAsync()
    {
        using var api = Factory.CreateApiClient(1030);
        var player = await CreatePlayerWithWorldAsync(api);
        var world = (await api.Worlds.GetOwnedWorldsAsync(CancellationToken.None)).Single();
        await api.Telemetry.SubmitAnalyticsAsync(
            new AnalyticsBatchRequest
            {
                Session = "creation-test",
                Platform = "web",
                Experiments = new Dictionary<string, string>(),
                Events =
                [
                    new AnalyticsEventRequest { Name = "player_deleted", Screen = "feed" },
                    new AnalyticsEventRequest { Name = "world_created", WorldId = world.Id, Screen = "feed" },
                    new AnalyticsEventRequest { Name = "world_audience_updated", WorldId = world.Id, Screen = "feed" },
                ],
            },
            CancellationToken.None);
        var events = Factory.ProductLogger.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == "BeginScope")
            .Select(call => call.GetArguments()[0])
            .OfType<IReadOnlyDictionary<string, object?>>()
            .Where(attributes => Equals(attributes.GetValueOrDefault("player_id"), player.Id) &&
                Equals(attributes.GetValueOrDefault("event_name"), "world_created"))
            .ToArray();
        await Assert.That(events.Length).IsEqualTo(1);
        await Assert.That(events[0]["world_id"]).IsEqualTo(world.Id);
        await Assert.That(events[0]["world"]).IsEqualTo(world.BalanceKey);
        await Assert.That(events[0].ContainsKey("registration_source")).IsFalse();
        var registration = Factory.ProductLogger.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == "BeginScope")
            .Select(call => call.GetArguments()[0])
            .OfType<IReadOnlyDictionary<string, object?>>()
            .Single(attributes => Equals(attributes.GetValueOrDefault("player_id"), player.Id) &&
                Equals(attributes.GetValueOrDefault("event_name"), "player_created"));
        await Assert.That(registration["registration_source"]).IsEqualTo("direct");
        var deleted = Factory.ProductLogger.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == "BeginScope")
            .Select(call => call.GetArguments()[0])
            .OfType<IReadOnlyDictionary<string, object?>>()
            .Any(attributes => Equals(attributes.GetValueOrDefault("player_id"), player.Id) &&
                Equals(attributes.GetValueOrDefault("event_name"), "player_deleted"));
        await Assert.That(deleted).IsFalse();
        await Assert.That(events[0].ContainsKey("TelegramUserId")).IsFalse();
        var audience = Factory.ProductLogger.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == "BeginScope")
            .Select(call => call.GetArguments()[0])
            .OfType<IReadOnlyDictionary<string, object?>>()
            .Single(attributes => Equals(attributes.GetValueOrDefault("world_id"), world.Id) &&
                Equals(attributes.GetValueOrDefault("event_name"), "world_audience_updated"));
        await Assert.That(audience["subscriber_total"]).IsEqualTo(world.Subscribers);
        await Assert.That(audience["activity_kind"]).IsEqualTo("inventory");
    }

    [Test]
    public async Task ClientLogs_InDevelopment_AreNotWrittenToMongoAsync()
    {
        using var api = Factory.CreateApiClient(1003);
        await CreatePlayerWithWorldAsync(api);

        await api.Telemetry.SubmitClientLogsAsync(
            new ClientLogBatchRequest
            {
                Session = "test-session",
                Entries =
                [
                    new ClientLogEntryRequest
                    {
                        Timestamp = 1,
                        Level = ClientLogLevel.Info,
                        Event = "test_event",
                        Screen = "feed",
                    },
                ],
            },
            CancellationToken.None);

        var logCount = await Database
            .GetCollection<BsonDocument>("logs")
            .CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
        await Assert.That(logCount).IsEqualTo(0);
    }
}

using System.Globalization;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Api.Requests.Telemetry;
using Reki.Application.Telemetry;

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
        var productEvents = Factory.Services.GetFakeLogCollector().GetSnapshot()
            .Where(record => record.Category == typeof(ProductTelemetry).FullName)
            .ToArray();
        var events = productEvents
            .Where(record => record.GetStructuredStateValue("player_id") == player.Id &&
                record.GetStructuredStateValue("event_name") == "world_created")
            .ToArray();
        await Assert.That(events.Length).IsEqualTo(1);
        await Assert.That(events[0].GetStructuredStateValue("world_id")).IsEqualTo(world.Id);
        await Assert.That(events[0].GetStructuredStateValue("world")).IsEqualTo(world.BalanceKey);
        await Assert.That(events[0].StructuredState?.Any(property => property.Key == "registration_source") ?? false).IsFalse();
        var registration = productEvents
            .Single(record => record.GetStructuredStateValue("player_id") == player.Id &&
                record.GetStructuredStateValue("event_name") == "player_created");
        await Assert.That(registration.GetStructuredStateValue("registration_source")).IsEqualTo("direct");
        var deleted = productEvents
            .Any(record => record.GetStructuredStateValue("player_id") == player.Id &&
                record.GetStructuredStateValue("event_name") == "player_deleted");
        await Assert.That(deleted).IsFalse();
        await Assert.That(events[0].StructuredState?.Any(property => property.Key == "TelegramUserId") ?? false).IsFalse();
        var audience = productEvents
            .Single(record => record.GetStructuredStateValue("world_id") == world.Id &&
                record.GetStructuredStateValue("event_name") == "world_audience_updated");
        await Assert.That(audience.GetStructuredStateValue("subscriber_total"))
            .IsEqualTo(world.Subscribers.ToString(CultureInfo.InvariantCulture));
        await Assert.That(audience.GetStructuredStateValue("activity_kind")).IsEqualTo("inventory");
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

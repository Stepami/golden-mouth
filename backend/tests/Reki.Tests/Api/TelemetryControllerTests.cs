using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Api.Requests.Telemetry.AnalyticsBatch;
using Reki.Api.Requests.Telemetry.ClientLogBatch;
using Reki.Api.Serialization;
using Reki.Application.Telemetry;
using Reki.Domain.Telemetry;

namespace Reki.Tests.Api;

[ClassDataSource<ApiFactory>(Shared = SharedType.PerTestSession)]
[ParallelGroup("Api", Order = 1)]
[NotInParallel("ApiState")]
public sealed class TelemetryControllerTests(ApiFactory factory) : GameApiTestBase(factory)
{
    [Test]
    public async Task StartSession_Anonymous_ReturnsDistinctGuidIdsAsync()
    {
        using var http = Factory.CreateClient();
        var client = new TelemetryClient(http);

        var first = await client.StartSessionAsync(CancellationToken.None);
        var second = await client.StartSessionAsync(CancellationToken.None);

        await Assert.That(first != Guid.Empty).IsTrue();
        await Assert.That(second != Guid.Empty).IsTrue();
        await Assert.That(second).IsNotEqualTo(first);
        await Assert.That(await Database.GetCollection<BsonDocument>("players").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).IsEqualTo(0L);
    }

    [Test]
    public async Task WorldCreated_ClientSpoof_LeavesSingleServerEventAsync()
    {
        using var api = Factory.CreateApiClient(1030);
        var player = await CreatePlayerWithWorldAsync(api);
        var world = (await api.Worlds.GetOwnedWorldsAsync(CancellationToken.None)).Single();
        await api.Telemetry.SubmitAnalyticsAsync(
            new AnalyticsBatchRequest
            {
                Session = SessionId.New(),
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
            .Where(record => record.GetStructuredStateValue("player_id") == player.Id.ToString() &&
                record.GetStructuredStateValue("event_name") == "world_created")
            .ToArray();
        await Assert.That(events.Length).IsEqualTo(1);
        await Assert.That(events[0].GetStructuredStateValue("world_id")).IsEqualTo(world.Id.ToString());
        await Assert.That(events[0].GetStructuredStateValue("template_id")).IsEqualTo((await GetStoredWorldAsync(1030))["TemplateId"].AsBsonBinaryData.ToGuid().ToString());
        await Assert.That(events[0].GetStructuredStateValue("difficulty")).IsEqualTo("easy");
        await Assert.That(events[0].StructuredState?.Any(property => property.Key == "registration_source") ?? false).IsFalse();
        var registration = productEvents
            .Single(record => record.GetStructuredStateValue("player_id") == player.Id.ToString() &&
                record.GetStructuredStateValue("event_name") == "player_created");
        await Assert.That(registration.GetStructuredStateValue("registration_source")).IsEqualTo("direct");
        var deleted = productEvents
            .Any(record => record.GetStructuredStateValue("player_id") == player.Id.ToString() &&
                record.GetStructuredStateValue("event_name") == "player_deleted");
        await Assert.That(deleted).IsFalse();
        await Assert.That(events[0].StructuredState?.Any(property => property.Key == "TelegramUserId") ?? false).IsFalse();
        var audience = productEvents
            .Single(record => record.GetStructuredStateValue("world_id") == world.Id.ToString() &&
                record.GetStructuredStateValue("event_name") == "world_audience_updated");
        await Assert.That(audience.GetStructuredStateValue("subscriber_total"))
            .IsEqualTo(world.Subscribers.ToString(CultureInfo.InvariantCulture));
        await Assert.That(audience.GetStructuredStateValue("activity_kind")).IsEqualTo("inventory");
    }

    [Test]
    public async Task LandingProfileLink_UsesAuthenticatedPlayerAsync()
    {
        using var api = Factory.CreateApiClient(1041);
        var player = await CreatePlayerWithWorldAsync(api);
        await api.Telemetry.SubmitAnalyticsAsync(LandingRequest("landing_profile_link", "infobiz"), CancellationToken.None);
        var link = Factory.Services.GetFakeLogCollector().GetSnapshot()
            .Single(record => record.Category == typeof(ProductTelemetry).FullName &&
                record.GetStructuredStateValue("event_name") == "landing_profile_link");
        await Assert.That(link.GetStructuredStateValue("player_id")).IsEqualTo(player.Id.ToString());
        await Assert.That(link.GetStructuredStateValue("activity_kind")).IsEqualTo("attribution");
    }

    [Test]
    [Arguments("worlds")]
    [Arguments("hogwarts")]
    [Arguments("infobiz")]
    [Arguments("evening")]
    [Arguments("houses")]
    public async Task Landing_Anonymous_DoesNotCountAsPlayerActivityAsync(string variant)
    {
        using var http = Factory.CreateClient();
        await new TelemetryClient(http).SubmitAnalyticsAsync(LandingRequest("landing_view", variant), CancellationToken.None);
        var entry = Factory.Services.GetFakeLogCollector().GetSnapshot()
            .Last(record => record.Category == typeof(ProductTelemetry).FullName &&
                record.GetStructuredStateValue("event_name") == "landing_view" &&
                record.GetStructuredStateValue("landing_variant") == variant);
        await Assert.That(entry.GetStructuredStateValue("activity_kind")).IsEqualTo("entry");
        await Assert.That(entry.StructuredState?.Any(property => property.Key is "player_id" or "unknown_field") ?? false).IsFalse();
        await Assert.That(await Database.GetCollection<BsonDocument>("players").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).IsEqualTo(0L);
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task Landing_Unauthorized_Returns401Async(bool invalidToken)
    {
        using var http = Factory.CreateClient();
        if (invalidToken)
        {
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
        }

        using var content = LandingContent(LandingRequest(invalidToken ? "landing_view" : "landing_profile_link", "worlds"));
        using var response = await http.PostAsync("/events", content);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    [Arguments("post_scored")]
    [Arguments("world_created")]
    public async Task Landing_Anonymous_IgnoresNonLandingEventsAsync(string name)
    {
        using var http = Factory.CreateClient();
        var before = Factory.Services.GetFakeLogCollector().GetSnapshot()
            .Count(record => record.Category == typeof(ProductTelemetry).FullName && record.GetStructuredStateValue("event_name") == name);
        await new TelemetryClient(http).SubmitAnalyticsAsync(LandingRequest(name, "worlds"), CancellationToken.None);
        await Assert.That(Factory.Services.GetFakeLogCollector().GetSnapshot()
            .Count(record => record.Category == typeof(ProductTelemetry).FullName && record.GetStructuredStateValue("event_name") == name)).IsEqualTo(before);
    }

    [Test]
    [Arguments("event_id", "\"invalid\"")]
    [Arguments("landing_variant", "\"unknown\"")]
    [Arguments("landing_switched", "\"no\"")]
    [Arguments("landing_source", "\"other\"")]
    [Arguments("landing_utm_campaign", "\"person@example.test\"")]
    [Arguments("t", "0")]
    public async Task Landing_InvalidField_Returns400Async(string field, string value)
    {
        using var http = Factory.CreateClient();
        var request = LandingRequest("landing_view", "worlds");
        var entry = request.Events[0];
        using var document = JsonDocument.Parse(value);
        var properties = entry.Props?.ToDictionary(item => item.Key, item => item.Value) ?? [];
        properties[field] = document.RootElement.Clone();
        request = request with
        {
            Events = [entry with { Props = properties, Timestamp = field == "t" ? 0 : entry.Timestamp }],
        };
        using var content = LandingContent(request);
        using var response = await http.PostAsync("/events", content);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ClientLogs_InDevelopment_AreNotWrittenToMongoAsync()
    {
        using var api = Factory.CreateApiClient(1003);
        await CreatePlayerWithWorldAsync(api);

        await api.Telemetry.SubmitClientLogsAsync(
            new ClientLogBatchRequest
            {
                Session = SessionId.New(),
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

    private static StringContent LandingContent(AnalyticsBatchRequest request) => new(
        JsonSerializer.Serialize(request, ApiJsonSerializerContext.Default.AnalyticsBatchRequest), Encoding.UTF8, "application/json");

    private static AnalyticsBatchRequest LandingRequest(string name, string variant)
    {
        using var properties = JsonDocument.Parse("""
            {"event_id":"33333333-3333-4333-8333-333333333333",
             "visitor_id":"11111111-1111-4111-8111-111111111111",
             "entry_id":"22222222-2222-4222-8222-222222222222",
             "landing_variant":"VARIANT","landing_initial_variant":"VARIANT",
             "landing_source":"link","landing_switched":false,
             "player_id":"untrusted","unknown_field":"not-exported"}
            """.Replace("VARIANT", variant, StringComparison.Ordinal));
        return new AnalyticsBatchRequest
        {
            Session = SessionId.New(), Platform = "web",
            Events =
            [
                new AnalyticsEventRequest
                {
                    Name = name, Screen = "landing", Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    Props = properties.RootElement.EnumerateObject().ToDictionary(property => property.Name, property => property.Value.Clone()),
                },
            ],
        };
    }
}

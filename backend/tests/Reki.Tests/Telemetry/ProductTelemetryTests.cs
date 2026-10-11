using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Reki.Application.Telemetry;
using Reki.Domain.Telemetry;
using Reki.Tests.Common;

namespace Reki.Tests.Telemetry;

public sealed class ProductTelemetryTests
{
    [Test]
    public async Task TrackSubmission_ValidEvent_AddsSafeProductAttributesAsync()
    {
        // Arrange
        var logger = Substitute.For<ILogger<ProductTelemetry>>();
        var telemetry = new ProductTelemetry(logger, new FakeTimeProvider());
        var submission = new AnalyticsSubmission(
            SessionId.From("session-1"),
            "ios",
            new Dictionary<string, string> { ["plus_offer"] = "10" },
            [
                new AnalyticsEntry(
                    TestGameData.CreateWorld().Id,
                    "screen_view",
                    123,
                    "feed",
                    "home",
                    new Dictionary<string, string>
                    {
                        ["id"] = "cta_main",
                        ["world"] = "academy",
                        ["message"] = "must not be exported",
                    }),
            ]);

        // Act
        telemetry.TrackSubmission(TestGameData.CreatePlayer(), submission);

        // Assert
        var scopeCall = logger.ReceivedCalls().Single(call =>
            call.GetMethodInfo().Name == nameof(ILogger.BeginScope));
        var attributes = scopeCall.GetArguments()[0] as IReadOnlyDictionary<string, object?>
            ?? throw new InvalidOperationException("The product telemetry scope was not structured.");
        await Assert.That(attributes["reki.stream"]).IsEqualTo("product");
        await Assert.That(attributes["event_name"]).IsEqualTo("screen_view");
        await Assert.That(attributes["player_id"])
            .IsEqualTo(TestGameData.CreatePlayer().Id.Value);
        await Assert.That(attributes["session_id"]).IsEqualTo("session-1");
        await Assert.That(attributes["prop_id"]).IsEqualTo("cta_main");
        await Assert.That(attributes["world"]).IsEqualTo("academy");
        await Assert.That(attributes["world_id"]).IsEqualTo(TestGameData.CreateWorld().Id.Value);
        await Assert.That(attributes.ContainsKey("prop_message")).IsFalse();
    }

    [Test]
    public async Task TrackSubmission_ActiveTime_ExportsOnlyValidChunksAsync()
    {
        var logger = Substitute.For<ILogger<ProductTelemetry>>();
        var telemetry = new ProductTelemetry(logger, new FakeTimeProvider());
        var player = TestGameData.CreatePlayer() with { AgeBand = "25-34" };
        var entries = new[] { "15000", "30001", "-1", "0", "text" }
            .Select(ms => new AnalyticsEntry(null, "active_time", 123, "feed", null, new Dictionary<string, string> { ["ms"] = ms })).ToArray();
        telemetry.TrackSubmission(player, new AnalyticsSubmission(SessionId.From("session-1"), "ios", new Dictionary<string, string>(), entries));
        var call = logger.ReceivedCalls().Single(item => item.GetMethodInfo().Name == nameof(ILogger.BeginScope));
        var attributes = call.GetArguments()[0] as IReadOnlyDictionary<string, object?>
            ?? throw new InvalidOperationException("Telemetry was not structured.");
        await Assert.That(attributes["age_group"]).IsEqualTo("25-34");
        await Assert.That(attributes["prop_ms"]).IsEqualTo("15000");
    }
}
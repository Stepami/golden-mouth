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
                    "screen_view",
                    123,
                    "feed",
                    "home",
                    new Dictionary<string, string>
                    {
                        ["id"] = "cta_main",
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
        await Assert.That(attributes.ContainsKey("prop_message")).IsFalse();
    }
}

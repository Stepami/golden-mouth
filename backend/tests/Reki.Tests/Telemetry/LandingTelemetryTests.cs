using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Reki.Application.Telemetry;

namespace Reki.Tests.Telemetry;

public sealed class LandingTelemetryTests
{
    private static LandingVisit Visit => new(
        Guid.Parse("11111111-1111-4111-8111-111111111111"),
        Guid.Parse("22222222-2222-4222-8222-222222222222"),
        "worlds",
        "worlds",
        "link",
        false,
        "campaign_a",
        "cpc",
        "ads",
        null);

    [Test]
    [Arguments("worlds")]
    [Arguments("hogwarts")]
    [Arguments("infobiz")]
    [Arguments("evening")]
    [Arguments("houses")]
    public async Task AnonymousEntryDoesNotBecomeAnActivePlayerAsync(string variant)
    {
        var log = new CaptureLogger();
        var clock = new FakeTimeProvider();
        new LandingTelemetry(log, clock).TrackEntry(Entry(clock, "landing_view") with
        {
            Visit = Visit with { Variant = variant, InitialVariant = variant },
        });
        var fields = log.Scopes.Single();
        await Assert.That(fields["landing_variant"]).IsEqualTo(variant);
        await Assert.That(fields["activity_kind"]).IsEqualTo("entry");
        await Assert.That(fields["landing_experiment"]).IsEqualTo("entry_v1");
        await Assert.That(fields.ContainsKey("player_id")).IsFalse();
        await Assert.That(fields["visitor_id"]).IsEqualTo(Visit.VisitorId.ToString("N"));
    }

    [Test]
    public async Task BrowserCannotSubmitServerMilestonesAsync()
    {
        var log = new CaptureLogger();
        var clock = new FakeTimeProvider();
        var writer = new LandingTelemetry(log, clock);
        writer.TrackEntry(Entry(clock, "post_scored"));
        writer.TrackEntry(Entry(clock, "landing_profile_link"));
        writer.TrackEntry(Entry(clock, "landing_first_post"));
        await Assert.That(log.Scopes.Count).IsEqualTo(0);
    }

    [Test]
    public async Task InvalidEntriesAreNotExportedAsync()
    {
        var log = new CaptureLogger();
        var clock = new FakeTimeProvider();
        var writer = new LandingTelemetry(log, clock);
        var entry = Entry(clock, "landing_view");
        writer.TrackEntry(entry with { Timestamp = entry.Timestamp + 600000 });
        writer.TrackEntry(entry with { Visit = Visit with { VisitorId = Guid.Empty } });
        writer.TrackEntry(entry with { Visit = Visit with { Variant = "other" } });
        await Assert.That(log.Scopes.Count).IsEqualTo(0);
    }

    [Test]
    public async Task PersonalDataCannotEnterCampaignAttributesAsync()
    {
        var log = new CaptureLogger();
        var clock = new FakeTimeProvider();
        new LandingTelemetry(log, clock).TrackEntry(
            Entry(clock, "landing_continue") with
            {
                Visit = Visit with { Campaign = "person@example.test", Content = "private text" },
            });
        var fields = log.Scopes.Single();
        await Assert.That(fields.ContainsKey("landing_utm_campaign")).IsFalse();
        await Assert.That(fields.ContainsKey("landing_utm_content")).IsFalse();
    }

    private static LandingEntryEvent Entry(TimeProvider clock, string name) => new(
        Guid.NewGuid(), name, clock.GetUtcNow().ToUnixTimeMilliseconds(), Visit, null);

    private sealed class CaptureLogger : ILogger<LandingTelemetry>
    {
        public List<IReadOnlyDictionary<string, object?>> Scopes { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            if (state is IReadOnlyDictionary<string, object?> fields)
            {
                Scopes.Add(new Dictionary<string, object?>(fields));
            }

            return null;
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
        }
    }
}

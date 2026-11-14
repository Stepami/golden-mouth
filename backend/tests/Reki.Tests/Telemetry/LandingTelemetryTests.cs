using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Reki.Application.RekiContext;
using Reki.Application.Telemetry;
using Reki.Application.Telemetry.SubmitAnalyticsCommand;
using Reki.Domain.Player;
using Reki.Domain.Telemetry;

namespace Reki.Tests.Telemetry;

public sealed class LandingTelemetryTests
{
    [Test]
    [Arguments("worlds")]
    [Arguments("hogwarts")]
    [Arguments("infobiz")]
    [Arguments("evening")]
    [Arguments("houses")]
    public async Task AnonymousEntryDoesNotLoadProfileOrBecomeAnActivePlayerAsync(string variant)
    {
        var log = new CaptureLogger();
        var accessor = Substitute.For<IRekiContextAccessor>();
        accessor.Context.Returns(_ => throw new InvalidOperationException("Anonymous event must not load a profile."));
        var telemetry = new ProductTelemetry(log, new FakeTimeProvider());
        var handler = new SubmitAnalyticsCommandHandler(telemetry, accessor);
        await handler.Handle(new SubmitAnalytics(Submission("landing_view", variant), Anonymous: true), CancellationToken.None);
        var fields = log.Scopes.Single();
        await Assert.That(fields["landing_variant"]).IsEqualTo(variant);
        await Assert.That(fields["activity_kind"]).IsEqualTo("entry");
        await Assert.That(fields.ContainsKey("player_id")).IsFalse();
    }

    [Test]
    public async Task ProfileLinkUsesServerIdentityAsync()
    {
        var log = new CaptureLogger();
        var player = new PlayerProfile
        {
            Id = PlayerId.From(Guid.NewGuid()), ExternalUserIds = [], BirthGroup = "18+", Address = Address.Nick,
            Wallet = PlayerWallet.Create(DateTimeOffset.UtcNow), Skills = new Dictionary<SkillId, SkillProgress>(),
            Settings = new PlayerSettings("all", "off", false),
        };
        new ProductTelemetry(log, new FakeTimeProvider()).TrackSubmission(player, Submission("landing_profile_link", "worlds"));
        var fields = log.Scopes.Single();
        await Assert.That(fields["player_id"]).IsEqualTo(player.Id.Value.ToString("N"));
        await Assert.That(fields["activity_kind"]).IsEqualTo("attribution");
        await Assert.That(fields["entry_event_source"]).IsEqualTo("verified_profile");
    }

    private static AnalyticsSubmission Submission(string name, string variant) => new(
        SessionId.New(),
        "web",
        LaunchSurface.Browser,
        new Dictionary<string, string>(),
        [
            new AnalyticsEntry(
                null,
                name,
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                string.Empty,
                null,
                new Dictionary<string, string>
                {
                    ["event_id"] = Guid.NewGuid().ToString(), ["visitor_id"] = Guid.NewGuid().ToString(),
                    ["entry_id"] = Guid.NewGuid().ToString(), ["landing_variant"] = variant,
                    ["landing_initial_variant"] = variant, ["landing_source"] = "link", ["landing_switched"] = "false",
                }),
        ]);

    private sealed class CaptureLogger : ILogger<ProductTelemetry>
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

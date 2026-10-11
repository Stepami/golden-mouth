using Microsoft.Extensions.Logging;
using Reki.Domain.Player;

namespace Reki.Application.Telemetry;

public sealed partial class ProductTelemetry(ILogger<ProductTelemetry> logger, TimeProvider timeProvider)
{
    private readonly int _maxAttributeLength = 64;
    private readonly HashSet<string> _events = new(StringComparer.Ordinal)
    {
        "active_time",
        "app_hidden",
        "app_visible",
        "card_earned",
        "challenge_finished",
        "challenge_post",
        "challenge_started",
        "crisis_shown",
        "event_answered",
        "hate_wave",
        "invite_converted",
        "invite_sent",
        "level_up",
        "limit_reached",
        "notification_opened",
        "notification_sent",
        "onboarding_step",
        "player_created",
        "plus_bonus_only",
        "plus_interest",
        "plus_shown",
        "post_publish",
        "post_result",
        "post_scored",
        "report",
        "screen_leave",
        "screen_view",
        "session_start",
        "share",
        "stage_reached",
        "tap",
    };

    private readonly HashSet<string> _properties = new(StringComparer.Ordinal)
    {
        "adult",
        "address",
        "brave",
        "card",
        "completed",
        "days",
        "delta",
        "flop",
        "humor",
        "id",
        "initiator_posts",
        "kind",
        "len",
        "level",
        "min_posts",
        "ms",
        "ms_on_screen",
        "ok",
        "opponent_posts",
        "own",
        "own_words",
        "post_index",
        "quality",
        "skill",
        "star",
        "step",
        "to",
        "topic",
        "viral",
        "world",
    };

    public void TrackSubmission(PlayerProfile player, AnalyticsSubmission submission)
    {
        foreach (var entry in submission.Events)
        {
            if (entry.Name == "active_time" &&
                (!entry.Properties.TryGetValue("ms", out var rawMs) ||
                 !long.TryParse(rawMs, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var ms) || ms is <= 0 or > 30000))
            {
                continue;
            }

            if (!_events.Contains(entry.Name))
            {
                continue;
            }

            var attributes = CreateAttributes(player.Id, entry.Name, entry.Timestamp);
            attributes["session_id"] = submission.Session.Value;
            attributes["platform"] = submission.Platform;
            attributes["screen"] = entry.Screen;
            attributes["world"] = player.World.Value;
            attributes["age_group"] = player.AgeBand ?? player.BirthGroup;
            if (entry.PreviousScreen is not null)
            {
                attributes["previous_screen"] = entry.PreviousScreen;
            }

            if (submission.Experiments.TryGetValue("plus_offer", out var plusOffer) &&
                IsSafeValue(plusOffer))
            {
                attributes["experiment_plus_offer"] = plusOffer;
            }

            AddProperties(attributes, entry.Properties);
            Write(entry.Name, attributes);
        }
    }

    public void Track(PlayerProfile player, string eventName, IReadOnlyDictionary<string, string> properties)
    {
        if (!_events.Contains(eventName))
        {
            return;
        }

        var attributes = CreateAttributes(player.Id, eventName, timeProvider.GetUtcNow().ToUnixTimeMilliseconds());
        attributes["world"] = player.World.Value;
        attributes["age_group"] = player.AgeBand ?? player.BirthGroup;
        AddProperties(attributes, properties);
        Write(eventName, attributes);
    }

    public void Track(
        PlayerId playerId,
        string eventName,
        IReadOnlyDictionary<string, string> properties)
    {
        if (!_events.Contains(eventName))
        {
            return;
        }

        var attributes = CreateAttributes(
            playerId,
            eventName,
            timeProvider.GetUtcNow().ToUnixTimeMilliseconds());
        AddProperties(attributes, properties);
        Write(eventName, attributes);
    }

    private Dictionary<string, object?> CreateAttributes(
        PlayerId playerId,
        string eventName,
        long timestamp)
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["reki.stream"] = "product",
            ["activity_kind"] = "real",
            ["event_name"] = eventName,
            ["event_time_unix_ms"] = timestamp,
            ["player_id"] = playerId.Value,
            ["received_time_unix_ms"] = timeProvider.GetUtcNow().ToUnixTimeMilliseconds(),
        };
    }

    private void AddProperties(
        IDictionary<string, object?> attributes,
        IReadOnlyDictionary<string, string> properties)
    {
        foreach ((var name, var value) in properties)
        {
            if (_properties.Contains(name) && IsSafeValue(value))
            {
                attributes[$"prop_{name}"] = value;
            }
        }
    }

    private bool IsSafeValue(string value)
    {
        return value.Length > 0 && value.Length <= _maxAttributeLength && value.All(character =>
            char.IsAsciiLetterOrDigit(character) || character is '_' or '-' or '.' or ':');
    }

    private void Write(string eventName, IReadOnlyDictionary<string, object?> attributes)
    {
        using var scope = logger.BeginScope(attributes);
        LogProductEvent(eventName);
    }

    [LoggerMessage(LogLevel.Information, "Product event {EventName}")]
    private partial void LogProductEvent(string eventName);
}

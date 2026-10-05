using Microsoft.Extensions.Logging;
using Reki.Domain.Player;
using Reki.Domain.Telemetry;

namespace Reki.Application.Telemetry;

public sealed class ProductTelemetry
{
    private readonly ILogger<ProductTelemetry> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly int _maxAttributeLength = 64;
    private readonly HashSet<string> _events = new(StringComparer.Ordinal)
    {
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

    public ProductTelemetry(ILogger<ProductTelemetry> logger, TimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public void TrackSubmission(PlayerProfile player, AnalyticsSubmission submission)
    {
        foreach (AnalyticsEntry entry in submission.Events)
        {
            if (!_events.Contains(entry.Name))
            {
                continue;
            }

            var attributes = CreateAttributes(player.Id, entry.Name, entry.Timestamp);
            attributes["session_id"] = submission.Session.Value;
            attributes["platform"] = submission.Platform;
            attributes["screen"] = entry.Screen;
            attributes["world"] = player.World.Value;
            attributes["age_group"] = player.BirthGroup;
            if (entry.PreviousScreen is not null)
            {
                attributes["previous_screen"] = entry.PreviousScreen;
            }

            if (submission.Experiments.TryGetValue("plus_offer", out string? plusOffer) &&
                IsSafeValue(plusOffer))
            {
                attributes["experiment_plus_offer"] = plusOffer;
            }

            AddProperties(attributes, entry.Properties);
            Write(entry.Name, attributes);
        }
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
            _timeProvider.GetUtcNow().ToUnixTimeMilliseconds());
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
            ["received_time_unix_ms"] = _timeProvider.GetUtcNow().ToUnixTimeMilliseconds(),
        };
    }

    private void AddProperties(
        IDictionary<string, object?> attributes,
        IReadOnlyDictionary<string, string> properties)
    {
        foreach ((string name, string value) in properties)
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
        using IDisposable? scope = _logger.BeginScope(attributes);
        _logger.LogInformation("Product event {EventName}", eventName);
    }
}

using Microsoft.Extensions.Logging;
using Reki.Application.Telemetry.SubmitAnalyticsCommand;
using Reki.Domain.Player;
using Reki.Domain.Worlds;

namespace Reki.Application.Telemetry;

internal sealed partial class ProductTelemetry(ILogger<ProductTelemetry> logger, TimeProvider timeProvider)
{
    private readonly int _maxAttributeLength = 64;
    private readonly HashSet<string> _events = new(StringComparer.Ordinal)
    {
        "active_time",
        "app_hidden",
        "app_visible",
        "card_earned",
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
        "player_deleted",
        "plus_bonus_only",
        "plus_interest",
        "plus_shown",
        "post_publish",
        "post_result",
        "post_scored",
        "post_failed",
        "report",
        "screen_leave",
        "screen_view",
        "session_start",
        "share",
        "stage_reached",
        "tap",
        "world_created",
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
        "quality",
        "publication_kind",
        "skill",
        "star",
        "step",
        "to",
        "topic",
        "viral",
        "world",
        "template_id",
        "difficulty",
    };

    public void TrackSubmission(PlayerProfile player, AnalyticsSubmission submission)
    {
        foreach (var entry in submission.Events)
        {
            if (entry.Name.StartsWith("landing_", StringComparison.Ordinal))
            {
                TrackLandingEntry(player, entry);
                continue;
            }

            if (entry.Name == "active_time" &&
                (!entry.Properties.TryGetValue("ms", out var rawMs) ||
                 !long.TryParse(rawMs, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var ms) || ms is <= 0 or > 30000))
            {
                continue;
            }

            if (entry.Name is "post_scored" or "player_created" or "player_deleted" or "world_created" or "level_up" or "card_earned" or "post_failed" || !_events.Contains(entry.Name))
            {
                continue;
            }

            var attributes = CreateAttributes(player.Id, entry.Name, entry.Timestamp);
            if (entry.WorldId is { } worldId)
            {
                attributes["world_id"] = worldId.ToString();
            }

            attributes["session_id"] = submission.Session.Value.ToString("N");
            attributes["platform"] = submission.Platform;
            attributes["launch_surface"] = submission.LaunchSurface switch
            {
                LaunchSurface.TelegramMiniApp => "telegram-mini-app",
                LaunchSurface.Browser => "browser",
                LaunchSurface.Standalone => "standalone",
                _ => "unknown",
            };
            attributes["screen"] = entry.Screen;
            attributes["difficulty"] = ReadDifficulty(entry.Properties);
            attributes["template_id"] = ReadTemplateId(entry.Properties);
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

    public void TrackPlayer(PlayerProfile player, string eventName, IReadOnlyDictionary<string, string> properties, WorldId? worldId = null, bool? referred = null)
    {
        if (!_events.Contains(eventName))
        {
            return;
        }

        var attributes = CreateAttributes(player.Id, eventName, timeProvider.GetUtcNow().ToUnixTimeMilliseconds());
        if (worldId is { } id)
        {
            attributes["world_id"] = id.ToString();
        }

        attributes["difficulty"] = ReadDifficulty(properties);
        attributes["template_id"] = ReadTemplateId(properties);
        attributes["age_group"] = player.AgeBand ?? player.BirthGroup;
        if (eventName == "player_created" && referred is { } isReferral)
        {
            attributes["registration_source"] = isReferral ? "referral" : "direct";
        }

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

    public void TrackWorldAudience(World world)
    {
        var attributes = CreateAttributes(world.Player.PlayerId, "world_audience_updated", timeProvider.GetUtcNow().ToUnixTimeMilliseconds());
        attributes["activity_kind"] = "inventory";
        attributes["world_id"] = world.Id.ToString();
        attributes["template_id"] = world.TemplateId?.ToString() ?? string.Empty;
        attributes["difficulty"] = world.Difficulty.ToString().ToLowerInvariant();
        attributes["subscriber_total"] = world.Player.Subscribers;
        Write("world_audience_updated", attributes);
    }

    public void TrackAnonymousSubmission(AnalyticsSubmission submission)
    {
        foreach (var entry in submission.Events)
        {
            TrackLandingEntry(null, entry);
        }
    }

    private static string ReadDifficulty(IReadOnlyDictionary<string, string> properties) =>
        properties.TryGetValue("difficulty", out var difficulty) && difficulty is "easy" or "normal" or "hard" ? difficulty : "unknown";

    private static string? ReadTemplateId(IReadOnlyDictionary<string, string> properties) =>
        properties.TryGetValue("template_id", out var template) && Guid.TryParse(template, out var id) ? id.ToString("D") : null;

    private void TrackLandingEntry(PlayerProfile? player, Domain.Telemetry.AnalyticsEntry entry)
    {
        var link = entry.Name == "landing_profile_link";
        if (link && player is null)
        {
            return;
        }

        var attributes = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["reki.stream"] = "product",
            ["event_name"] = entry.Name,
            ["activity_kind"] = link ? "attribution" : "entry",
            ["entry_event_source"] = link ? "verified_profile" : "browser",
            ["landing_experiment"] = "entry_v1",
            ["event_time_unix_ms"] = entry.Timestamp,
            ["received_time_unix_ms"] = timeProvider.GetUtcNow().ToUnixTimeMilliseconds(),
        };
        foreach (var key in new[]
        {
            "event_id", "visitor_id", "entry_id", "landing_variant", "landing_initial_variant",
            "landing_source", "landing_utm_campaign", "landing_utm_medium", "landing_utm_source", "landing_utm_content", "entry_post_id",
        })
        {
            if (entry.Properties.TryGetValue(key, out var value))
            {
                attributes[key] = key is "visitor_id" or "entry_id" or "event_id" or "entry_post_id"
                    ? Guid.Parse(value).ToString("N")
                    : value;
            }
        }

        attributes["landing_switched"] = bool.Parse(entry.Properties["landing_switched"]);
        if (link && player is { } linkedPlayer)
        {
            attributes["player_id"] = linkedPlayer.Id.Value.ToString("N");
            attributes["age_group"] = linkedPlayer.AgeBand ?? linkedPlayer.BirthGroup;
        }

        Write(entry.Name, attributes);
    }

    private Dictionary<string, object?> CreateAttributes(
        PlayerId playerId,
        string eventName,
        long timestamp)
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["reki.stream"] = "product",
            ["activity_kind"] = eventName == "player_deleted" ? "lifecycle" : "real",
            ["event_name"] = eventName,
            ["event_time_unix_ms"] = timestamp,
            ["player_id"] = playerId.Value.ToString("N"),
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

using Microsoft.Extensions.Logging;
using Reki.Domain.Player;

namespace Reki.Application.Telemetry;

internal sealed partial class LandingTelemetry(ILogger<LandingTelemetry> logger, TimeProvider timeProvider)
{
    public void TrackEntry(LandingEntryEvent entry)
    {
        var received = timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
        if (entry.EventId == Guid.Empty ||
            entry.Name is not ("landing_view" or "landing_continue" or "landing_auth_complete" or
                "landing_world_selected" or "landing_character_selected" or "landing_reaction_viewed") ||
            entry.Timestamp < received - 86400000 || entry.Timestamp > received + 300000 ||
            !IsValid(entry.Visit))
        {
            return;
        }

        var attributes = Attributes(entry.Visit, entry.Name, entry.Timestamp, received);
        attributes["activity_kind"] = "entry";
        attributes["event_id"] = entry.EventId.ToString("N");
        attributes["entry_event_source"] = "browser";
        if (entry.PostId is { } postId && postId != Guid.Empty)
        {
            attributes["entry_post_id"] = postId.ToString("N");
        }

        Write(entry.Name, attributes);
    }

    public void LinkProfile(PlayerProfile player, LandingVisit visit)
    {
        if (!IsValid(visit))
        {
            return;
        }

        var received = timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
        var attributes = Attributes(visit, "landing_profile_link", received, received);
        attributes["activity_kind"] = "attribution";
        attributes["entry_event_source"] = "verified_profile";
        attributes["player_id"] = player.Id.Value.ToString("N");
        attributes["age_group"] = player.AgeBand ?? player.BirthGroup;
        Write("landing_profile_link", attributes);
    }

    private static bool IsValid(LandingVisit visit) =>
        visit.VisitorId != Guid.Empty && visit.EntryId != Guid.Empty &&
        visit.Variant is "worlds" or "hogwarts" or "infobiz" or "evening" or "houses" &&
        visit.InitialVariant is "worlds" or "hogwarts" or "infobiz" or "evening" or "houses" &&
        visit.Source is "link" or "direct";

    private static Dictionary<string, object?> Attributes(
        LandingVisit visit, string name, long timestamp, long received)
    {
        var attributes = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["reki.stream"] = "product",
            ["event_name"] = name,
            ["visitor_id"] = visit.VisitorId.ToString("N"),
            ["entry_id"] = visit.EntryId.ToString("N"),
            ["landing_experiment"] = "entry_v1",
            ["landing_variant"] = visit.Variant,
            ["landing_initial_variant"] = visit.InitialVariant,
            ["landing_source"] = visit.Source,
            ["landing_switched"] = visit.Switched,
            ["event_time_unix_ms"] = timestamp,
            ["received_time_unix_ms"] = received,
        };
        AddCode(attributes, "landing_utm_campaign", visit.Campaign);
        AddCode(attributes, "landing_utm_medium", visit.Medium);
        AddCode(attributes, "landing_utm_source", visit.TrafficSource);
        AddCode(attributes, "landing_utm_content", visit.Content);
        return attributes;
    }

    private static void AddCode(IDictionary<string, object?> attributes, string name, string? value)
    {
        if (value is { Length: > 0 and <= 64 } &&
            value.All(character => char.IsAsciiLetterOrDigit(character) || character is '_' or '-' or '.'))
        {
            attributes[name] = value;
        }
    }

    private void Write(string name, IReadOnlyDictionary<string, object?> attributes)
    {
        using var scope = logger.BeginScope(attributes);
        LogLandingEvent(name);
    }

    [LoggerMessage(LogLevel.Information, "Landing event {EventName}")]
    private partial void LogLandingEvent(string eventName);
}

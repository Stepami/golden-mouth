using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Reki.Domain.Worlds;

namespace Reki.Api.Requests.Telemetry.AnalyticsBatch;

public sealed record AnalyticsEventRequest : IValidatableObject
{
    /// <summary>
    /// Идентификатор экземпляра мира; отсутствует у событий до создания мира.
    /// </summary>
    public WorldId? WorldId { get; init; }

    [Required]
    [MaxLength(64)]
    public required string Name { get; init; }

    [System.Text.Json.Serialization.JsonPropertyName("t")]
    public long Timestamp { get; init; }

    [MaxLength(64)]
    public string Screen { get; init; } = string.Empty;

    [MaxLength(64)]
    public string? Prev { get; init; }

    public IReadOnlyDictionary<string, JsonElement>? Props { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Name?.StartsWith("landing_", StringComparison.Ordinal) != true)
        {
            yield break;
        }

        var allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            "event_id", "visitor_id", "entry_id", "landing_variant", "landing_initial_variant",
            "landing_source", "landing_switched", "landing_utm_campaign", "landing_utm_medium",
            "landing_utm_source", "landing_utm_content", "entry_post_id",
        };
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (Name is not ("landing_view" or "landing_continue" or "landing_auth_complete" or
            "landing_world_selected" or "landing_character_selected" or "landing_reaction_viewed" or "landing_profile_link") ||
            WorldId is not null || Props is null || Props.Keys.Any(key => !allowed.Contains(key)) ||
            Timestamp < now - 86400000 || Timestamp > now + 300000 ||
            !ValidId("event_id") || !ValidId("visitor_id") || !ValidId("entry_id") ||
            Read("landing_variant") is not ("worlds" or "hogwarts" or "infobiz" or "evening" or "houses") ||
            Read("landing_initial_variant") is not ("worlds" or "hogwarts" or "infobiz" or "evening" or "houses") ||
            Read("landing_source") is not ("link" or "direct") ||
            !bool.TryParse(Read("landing_switched"), out _) ||
            (Props.ContainsKey("entry_post_id") && !ValidId("entry_post_id")) ||
            Props.Where(item => item.Key.StartsWith("landing_utm_", StringComparison.Ordinal)).Any(item =>
                item.Value.ValueKind != JsonValueKind.String || item.Value.GetString() is not { Length: > 0 and <= 64 } code ||
                !code.All(character => char.IsAsciiLetterOrDigit(character) || character is '_' or '-' or '.')))
        {
            yield return new ValidationResult("Invalid landing event.", [nameof(Props)]);
        }

        string? Read(string key) => Props?.TryGetValue(key, out var value) == true
            ? value.ValueKind switch
            {
                JsonValueKind.String => value.GetString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => null,
            }
            : null;

        bool ValidId(string key) => Guid.TryParse(Read(key), out var id) && id != Guid.Empty;
    }
}

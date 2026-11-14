using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Reki.Application.Telemetry.SubmitAnalyticsCommand;
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

        if (!SubmitAnalytics.IsLandingEvent(Name) && Name != "landing_profile_link")
        {
            yield return new ValidationResult("Unknown landing event.", [nameof(Name)]);
        }

        if (WorldId is not null)
        {
            yield return new ValidationResult("Landing events do not refer to a player world.", [nameof(WorldId)]);
        }

        if (Props is null)
        {
            yield return new ValidationResult("Landing properties are required.", [nameof(Props)]);
            yield break;
        }

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (Timestamp < now - 86400000 || Timestamp > now + 300000)
        {
            yield return new ValidationResult("Landing timestamp is outside the accepted window.", [nameof(Timestamp)]);
        }

        foreach (var key in new[] { "event_id", "visitor_id", "entry_id", "entry_post_id" })
        {
            if (key == "entry_post_id" && !Props.ContainsKey(key))
            {
                continue;
            }

            if (!Guid.TryParse(Read(key), out var id) || id == Guid.Empty)
            {
                yield return new ValidationResult("A nonempty UUID is required.", [$"Props.{key}"]);
            }
        }

        foreach (var key in new[] { "landing_variant", "landing_initial_variant" }
            .Where(key => Read(key) is not ("worlds" or "hogwarts" or "infobiz" or "evening" or "houses")))
        {
            yield return new ValidationResult("Unknown landing variant.", [$"Props.{key}"]);
        }

        if (Read("landing_source") is not ("link" or "direct"))
        {
            yield return new ValidationResult("Unknown landing source.", ["Props.landing_source"]);
        }

        if (!bool.TryParse(Read("landing_switched"), out _))
        {
            yield return new ValidationResult("A boolean is required.", ["Props.landing_switched"]);
        }

        foreach (var key in new[] { "landing_utm_campaign", "landing_utm_medium", "landing_utm_source", "landing_utm_content" })
        {
            if (Props.TryGetValue(key, out var value) &&
                (value.ValueKind != JsonValueKind.String || value.GetString() is not { Length: > 0 and <= 64 } code ||
                 !code.All(character => char.IsAsciiLetterOrDigit(character) || character is '_' or '-' or '.')))
            {
                yield return new ValidationResult("UTM must be a short campaign code.", [$"Props.{key}"]);
            }
        }

        string? Read(string key) => Props.TryGetValue(key, out var value)
            ? value.ValueKind switch
            {
                JsonValueKind.String => value.GetString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => null,
            }
            : null;
    }
}

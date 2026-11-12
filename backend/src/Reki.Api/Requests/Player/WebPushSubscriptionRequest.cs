using System.ComponentModel.DataAnnotations;

namespace Reki.Api.Requests.Player;

public sealed record WebPushSubscriptionRequest : IValidatableObject
{
    public required string Endpoint { get; init; }

    public required string P256dh { get; init; }

    public required string Auth { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
            !uri.IsDefaultPort || !string.IsNullOrEmpty(uri.UserInfo) ||
            !(uri.Host == "fcm.googleapis.com" || uri.Host == "updates.push.services.mozilla.com" ||
              uri.Host.EndsWith(".push.apple.com", StringComparison.OrdinalIgnoreCase) ||
              uri.Host.EndsWith(".notify.windows.com", StringComparison.OrdinalIgnoreCase)))
        {
            yield return new ValidationResult("Invalid push endpoint.", [nameof(Endpoint)]);
        }
    }
}
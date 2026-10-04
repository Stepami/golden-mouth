using System.Diagnostics.CodeAnalysis;

namespace Reki.Infrastructure.Observability;

public sealed class OpenTelemetryOptions
{
    public const string SectionName = "OpenTelemetry";

    public bool Enabled { get; set; }

    public required string Endpoint { get; set; }

    public bool TryGetEndpoint(
        [NotNullWhen(true)] out Uri? uri)
    {
        return Uri.TryCreate(Endpoint, UriKind.Absolute, out uri) &&
               (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));
    }
}

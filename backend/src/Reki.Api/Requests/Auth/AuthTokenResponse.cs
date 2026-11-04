using System.Text.Json.Serialization;

namespace Reki.Api.Requests.Auth;

public sealed record AuthTokenResponse(
    [property: JsonPropertyName("token_type")] string TokenType,
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("expires_in")] long ExpiresIn,
    [property: JsonPropertyName("refresh_token")] string RefreshToken);

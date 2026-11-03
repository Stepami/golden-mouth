using Microsoft.AspNetCore.Mvc;

namespace Reki.Api.Requests.Auth;

public sealed class RefreshTokenRequest
{
    [FromForm(Name = "client_id")]
    public required string ClientId { get; init; }

    [FromForm(Name = "grant_type")]
    public required string GrantType { get; init; }

    [FromForm(Name = "refresh_token")]
    public required string RefreshToken { get; init; }
}

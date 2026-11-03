using Microsoft.AspNetCore.Mvc;

namespace Reki.Api.Requests.Auth;

public sealed class TokenExchangeRequest
{
    [FromForm(Name = "client_id")]
    public required string ClientId { get; init; }

    [FromForm(Name = "grant_type")]
    public required string GrantType { get; init; }
}

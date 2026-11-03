using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Reki.Api.Requests.Auth;

public sealed class LogoutRequest
{
    [FromForm(Name = "refresh_token")]
    [JsonPropertyName("refresh_token")]
    public required string RefreshToken { get; init; }
}

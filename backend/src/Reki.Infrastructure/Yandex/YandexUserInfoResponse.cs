using System.Text.Json.Serialization;

namespace Reki.Infrastructure.Yandex;

public sealed record YandexUserInfoResponse(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("client_id")] string? ClientId);

using System.Text.Json.Serialization;

namespace Reki.Infrastructure.Vk;

public sealed record VkUserInfoResponse([property: JsonPropertyName("user")] VkUserInfoResponse.VkUser? User)
{
    public sealed record VkUser([property: JsonPropertyName("user_id")] string? UserId);
}

using System.Text.Json.Serialization;

namespace Reki.Infrastructure.Vk;

internal sealed record VkUserInfoResponse(VkUserInfoResponse.VkUser? User)
{
    internal sealed record VkUser([property: JsonPropertyName("user_id")] string? UserId);
}

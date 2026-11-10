using System.Text.Json.Serialization;

namespace Reki.Infrastructure.Vk;

[JsonSerializable(typeof(VkUserInfoResponse))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(string))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
internal sealed partial class VkJsonSerializerContext : JsonSerializerContext;

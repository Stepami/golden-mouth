using System.Text.Json.Serialization;

namespace Reki.Application.Posts;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(StarPlayerPromptData))]
[JsonSerializable(typeof(string[]))]
internal sealed partial class StarPromptJsonContext : JsonSerializerContext;
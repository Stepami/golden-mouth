using System.Text.Json.Serialization;
using Reki.Application.Social;

namespace Reki.Application.Posts;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(StarPlayerPromptData))]
[JsonSerializable(typeof(StarPersona[]))]
[JsonSerializable(typeof(string[]))]
internal sealed partial class StarPromptJsonContext : JsonSerializerContext;

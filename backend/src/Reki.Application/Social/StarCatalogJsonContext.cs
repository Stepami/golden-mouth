using System.Text.Json.Serialization;

namespace Reki.Application.Social;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(StarPersona[]))]
internal sealed partial class StarCatalogJsonContext : JsonSerializerContext;

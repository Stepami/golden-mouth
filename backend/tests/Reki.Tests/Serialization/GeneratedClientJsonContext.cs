using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Reki.Tests.Serialization;

[JsonSerializable(typeof(ProblemDetails))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal sealed partial class GeneratedClientJsonContext : JsonSerializerContext;

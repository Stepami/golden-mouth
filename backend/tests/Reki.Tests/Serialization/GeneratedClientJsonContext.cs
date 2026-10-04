using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Reki.Contracts.Example;

namespace Reki.Tests.Serialization;

[JsonSerializable(typeof(ExampleResponse))]
[JsonSerializable(typeof(ProblemDetails))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal sealed partial class GeneratedClientJsonContext : JsonSerializerContext;

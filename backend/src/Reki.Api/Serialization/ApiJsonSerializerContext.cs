using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Reki.Contracts.Example;

namespace Reki.Api.Serialization;

[JsonSerializable(typeof(ExampleResponse))]
[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(HttpValidationProblemDetails))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
public sealed partial class ApiJsonSerializerContext : JsonSerializerContext;

using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Reki.Api.Serialization;

[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(HttpValidationProblemDetails))]
[JsonSerializable(typeof(ValidationProblemDetails))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal sealed partial class ProblemDetailsJsonSerializerContext : JsonSerializerContext;

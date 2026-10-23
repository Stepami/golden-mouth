using System.Text.Json;
using System.Text.Json.Serialization;

namespace Reki.Api.Serialization;

public sealed class SnakeCaseEnumConverter<TEnum>() :
    JsonStringEnumConverter<TEnum>(JsonNamingPolicy.SnakeCaseLower)
    where TEnum : struct, Enum;

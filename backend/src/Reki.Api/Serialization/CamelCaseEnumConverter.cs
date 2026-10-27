using System.Text.Json;
using System.Text.Json.Serialization;

namespace Reki.Api.Serialization;

public sealed class CamelCaseEnumConverter<TEnum>() :
    JsonStringEnumConverter<TEnum>(JsonNamingPolicy.CamelCase)
    where TEnum : struct, Enum;

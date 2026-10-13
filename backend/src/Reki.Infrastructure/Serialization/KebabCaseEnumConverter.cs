using System.Text.Json;
using System.Text.Json.Serialization;

namespace Reki.Infrastructure.Serialization;

public sealed class KebabCaseEnumConverter<TEnum>() :
    JsonStringEnumConverter<TEnum>(JsonNamingPolicy.KebabCaseLower)
    where TEnum : struct, Enum;

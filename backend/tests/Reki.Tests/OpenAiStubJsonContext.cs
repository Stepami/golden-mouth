using System.Text.Json;
using System.Text.Json.Serialization;

namespace Reki.Tests;

[JsonSerializable(typeof(Dictionary<string, object?>))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(JsonElement))]
internal sealed partial class OpenAiStubJsonContext : JsonSerializerContext;

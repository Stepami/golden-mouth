using System.Text.Json.Serialization;

namespace Reki.Infrastructure.AI;

[JsonSerializable(typeof(LlmSpendLog[]))]
internal partial class LlmSpendJsonContext : JsonSerializerContext;

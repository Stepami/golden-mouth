using System.Text.Json.Serialization;

namespace Reki.Infrastructure.AI;

[JsonSerializable(typeof(IReadOnlyList<string>))]
internal sealed partial class ContentReviewJsonContext : JsonSerializerContext;

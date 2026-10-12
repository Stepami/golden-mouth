using System.Text.Json.Serialization;
using Reki.Domain.Posts;

namespace Reki.Tests.Posts;

[JsonSerializable(typeof(AuthorId))]
internal sealed partial class AuthorTestJsonContext : JsonSerializerContext;

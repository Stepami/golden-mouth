using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.Responses.ReplyGeneration;

public sealed record ReplyGenerationLiveEvent(
    ReplyGenerationStatus Status,
    AuthorId AuthorId,
    string? Text,
    StarId? AdditionalStarId,
    string? Code);

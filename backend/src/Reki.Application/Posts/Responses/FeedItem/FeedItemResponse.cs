using Reki.Domain.Posts;

namespace Reki.Application.Posts.Responses.FeedItem;

public sealed record FeedItemResponse(
    FeedItemKind Kind,
    PostId? PostId,
    string? Author,
    string? Avatar,
    string? Role,
    string? Ago,
    string Text,
    long? Likes,
    IReadOnlyList<AuthorId>? LikedBy,
    long? Views,
    int? CommentCount,
    string? Repost,
    PostId? RepostId = null,
    AuthorId? AuthorId = null);

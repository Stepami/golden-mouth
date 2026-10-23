namespace Reki.Application.Posts.Responses.FeedItem;

public sealed record FeedItemResponse(
    FeedItemKind Kind,
    string? PostId,
    string? Author,
    string? Avatar,
    string? Role,
    string? Ago,
    string Text,
    IReadOnlyList<ReactionResponse>? Reactions,
    long? Views,
    int? CommentCount,
    string? Repost);

namespace Reki.Contracts.Game.Posts;

public sealed record FeedItemResponse(
    FeedItemKind Kind,
    string? Author,
    string? Avatar,
    string? Role,
    string? Ago,
    string Text,
    IReadOnlyList<ReactionResponse>? Reactions,
    string? Repost);


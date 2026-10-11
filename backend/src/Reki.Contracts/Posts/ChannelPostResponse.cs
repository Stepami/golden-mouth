namespace Reki.Contracts.Posts;

public sealed record ChannelPostResponse(
    string PostId,
    string Text,
    DateTimeOffset CreatedAtUtc,
    long Views,
    IReadOnlyList<ReactionResponse> Reactions,
    int CommentCount);

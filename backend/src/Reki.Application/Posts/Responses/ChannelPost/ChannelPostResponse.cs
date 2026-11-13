using Reki.Domain.Posts;

namespace Reki.Application.Posts.Responses.ChannelPost;

public sealed record ChannelPostResponse(
    PostId PostId,
    string Text,
    DateTimeOffset CreatedAtUtc,
    long Views,
    IReadOnlyList<ReactionResponse> Reactions,
    int CommentCount,
    QuotedPostResponse? Quote = null,
    bool IsRepost = false);

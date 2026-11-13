using Reki.Domain.Posts;

namespace Reki.Application.Posts.Responses.ChannelPost;

public sealed record ChannelPostResponse(
    PostId PostId,
    string Text,
    DateTimeOffset CreatedAtUtc,
    long Views,
    long Likes,
    IReadOnlyList<AuthorId> LikedBy,
    int CommentCount,
    PostId? RepostId = null);

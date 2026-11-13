using Reki.Application.Posts.Responses.Comment;
using Reki.Domain.Posts;

namespace Reki.Application.Posts.Responses.PostDetail;

public sealed record PostDetailResponse(
    PostId PostId,
    AuthorId AuthorId,
    string Text,
    long Views,
    long Likes,
    IReadOnlyList<AuthorId> LikedBy,
    IReadOnlyList<CommentResponse> Comments,
    PostId? RepostId = null);

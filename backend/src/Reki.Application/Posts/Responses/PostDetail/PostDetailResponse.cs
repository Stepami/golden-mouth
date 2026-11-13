using Reki.Application.Posts.Responses.Comment;
using Reki.Domain.Posts;

namespace Reki.Application.Posts.Responses.PostDetail;

public sealed record PostDetailResponse(
    PostId PostId,
    AuthorId AuthorId,
    string Text,
    long Views,
    IReadOnlyList<ReactionResponse> Reactions,
    IReadOnlyList<CommentResponse> Comments,
    QuotedPostResponse? Quote = null,
    bool IsRepost = false);

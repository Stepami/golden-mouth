using Reki.Application.Posts.Responses.PostDetail;
using Reki.Domain.Posts;

namespace Reki.Application.Posts.Responses.Reply;

public sealed record ReplyResponse(
    CommentId CommentId,
    AuthorId AuthorId,
    PostDetailResponse Post,
    int EnergyLeft,
    long Subscribers);

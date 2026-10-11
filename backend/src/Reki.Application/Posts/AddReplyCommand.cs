using Mediator;
using Reki.Contracts.Posts;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts;

public sealed record AddReplyCommand(
    PostId PostId,
    CommentId TargetCommentId,
    string ReplyId,
    string Text,
    WorldId WorldId) : IRequest<ReplyResponse>;
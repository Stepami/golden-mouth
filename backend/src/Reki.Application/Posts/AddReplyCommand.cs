using Mediator;
using Reki.Contracts.Posts;
using Reki.Domain.Player;
using Reki.Domain.Posts;

namespace Reki.Application.Posts;

public sealed record AddReplyCommand(
    TelegramUserId TelegramUserId,
    PostId PostId,
    CommentId TargetCommentId,
    string ReplyId,
    string Text) : IRequest<ReplyResponse>;


using Mediator;
using Reki.Application.Posts.Responses.Reply;
using Reki.Application.Telemetry;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.AddReplyCommand;

public sealed record AddReply(
    PostId PostId,
    CommentId? TargetCommentId,
    AuthorId AuthorId,
    string RequestId,
    string Text,
    WorldId WorldId) : ICommand<ProductGoalResult<ReplyResponse>>;

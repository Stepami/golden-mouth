using Mediator;
using Reki.Contracts.Posts;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.AddReplyCommand;

public sealed record AddReply(
    PostId PostId,
    CommentId? TargetCommentId,
    string RequestId,
    string Text,
    WorldId WorldId) : IStreamCommand<ReplyLiveEvent>;

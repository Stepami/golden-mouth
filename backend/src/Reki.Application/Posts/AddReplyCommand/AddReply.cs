using Mediator;
using Reki.Application.Posts.Responses;
using Reki.Application.Posts.Responses.ReplyLiveEvent;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;
using ReplyLiveEvent = Reki.Application.Posts.Responses.ReplyLiveEvent.ReplyLiveEvent;

namespace Reki.Application.Posts.AddReplyCommand;

public sealed record AddReply(
    PostId PostId,
    CommentId? TargetCommentId,
    string RequestId,
    string Text,
    WorldId WorldId) : IStreamCommand<ReplyLiveEvent>;

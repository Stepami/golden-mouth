using Mediator;
using Reki.Application.Player.Challenges;
using Reki.Application.Posts.Responses.Reply;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.AddReplyCommand;

public sealed record AddReply(
    PostId PostId,
    CommentId? TargetCommentId,
    AuthorId AuthorId,
    string RequestId,
    string Text,
    WorldId WorldId) : ICommand<ReplyResponse>, ITrackDailyProgress;

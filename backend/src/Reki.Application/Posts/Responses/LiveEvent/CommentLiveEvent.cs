using Reki.Application.Posts.Responses.Comment;

namespace Reki.Application.Posts.Responses.LiveEvent;

public sealed record CommentLiveEvent(CommentResponse Comment) : LiveEvent(LiveEventType.Comment);


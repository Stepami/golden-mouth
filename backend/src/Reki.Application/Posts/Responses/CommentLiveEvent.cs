namespace Reki.Application.Posts.Responses;

public sealed record CommentLiveEvent(CommentResponse Comment) : LiveEvent(LiveEventType.Comment);


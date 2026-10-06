namespace Reki.Contracts.Posts;

public sealed record CommentLiveEvent(CommentResponse Comment) : LiveEvent(LiveEventType.Comment);


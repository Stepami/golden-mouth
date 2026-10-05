namespace Reki.Contracts.Posts;

public sealed record CommentLiveEvent(LiveEventType Type, CommentResponse Comment);


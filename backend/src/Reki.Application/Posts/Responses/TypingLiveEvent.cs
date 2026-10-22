namespace Reki.Application.Posts.Responses;

public sealed record TypingLiveEvent(string Who) : LiveEvent(LiveEventType.Typing);


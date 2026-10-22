namespace Reki.Application.Posts.Responses.LiveEvent;

public sealed record TypingLiveEvent(string Who) : LiveEvent(LiveEventType.Typing);


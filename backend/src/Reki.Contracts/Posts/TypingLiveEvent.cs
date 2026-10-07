namespace Reki.Contracts.Posts;

public sealed record TypingLiveEvent(string Who) : LiveEvent(LiveEventType.Typing);


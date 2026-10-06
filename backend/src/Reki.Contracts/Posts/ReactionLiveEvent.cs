namespace Reki.Contracts.Posts;

public sealed record ReactionLiveEvent(string Emoji) : LiveEvent(LiveEventType.React);


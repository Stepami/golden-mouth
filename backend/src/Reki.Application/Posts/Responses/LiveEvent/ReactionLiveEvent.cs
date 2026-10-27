namespace Reki.Application.Posts.Responses.LiveEvent;

public sealed record ReactionLiveEvent(string Emoji) : LiveEvent(LiveEventType.React);


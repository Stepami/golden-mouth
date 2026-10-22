namespace Reki.Application.Posts.Responses;

public sealed record ReactionLiveEvent(string Emoji) : LiveEvent(LiveEventType.React);


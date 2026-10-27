namespace Reki.Application.Posts.Responses.LiveEvent;

public sealed record ViewsLiveEvent(long Views) : LiveEvent(LiveEventType.Views);


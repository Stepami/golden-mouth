namespace Reki.Application.Posts.Responses;

public sealed record ViewsLiveEvent(long Views) : LiveEvent(LiveEventType.Views);


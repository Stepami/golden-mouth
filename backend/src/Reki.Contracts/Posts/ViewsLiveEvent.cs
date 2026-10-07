namespace Reki.Contracts.Posts;

public sealed record ViewsLiveEvent(long Views) : LiveEvent(LiveEventType.Views);


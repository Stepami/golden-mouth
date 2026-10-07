namespace Reki.Contracts.Posts;

public sealed record ResultLiveEvent(PostResultResponse Result) : LiveEvent(LiveEventType.Result);


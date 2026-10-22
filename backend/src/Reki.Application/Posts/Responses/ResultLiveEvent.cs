namespace Reki.Application.Posts.Responses;

public sealed record ResultLiveEvent(PostResultResponse Result) : LiveEvent(LiveEventType.Result);


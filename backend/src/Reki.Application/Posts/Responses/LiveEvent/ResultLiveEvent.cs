using Reki.Application.Posts.Responses.PostResult;

namespace Reki.Application.Posts.Responses.LiveEvent;

public sealed record ResultLiveEvent(PostResultResponse Result) : LiveEvent(LiveEventType.Result);


using Reki.Domain.Posts;

namespace Reki.Application.Posts.Responses;

public sealed record RepostResponse(PostId? PostId, bool Active, long SubscriberDelta, int EnergyLeft, long Subscribers);

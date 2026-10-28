using Reki.Domain.Player;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.AI.Feed;

public sealed record ScheduleFromFeedRequest(
    PlayerId PlayerId,
    WorldId WorldId);

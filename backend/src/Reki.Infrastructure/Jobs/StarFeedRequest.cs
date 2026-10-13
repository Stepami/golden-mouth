using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Infrastructure.Jobs;

public sealed record StarFeedRequest(PlayerId PlayerId, WorldId WorldId, PostId PostId, string Trigger, string TriggerId);

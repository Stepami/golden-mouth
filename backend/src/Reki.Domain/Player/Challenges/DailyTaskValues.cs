using Reki.Domain.Worlds;

namespace Reki.Domain.Player.Challenges;

public sealed record DailyTaskValues(long Target, long Progress, StarId? StarId = null);

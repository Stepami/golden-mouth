using Reki.Domain.Worlds;

namespace Reki.Domain.Player.Challenges;

public sealed record PlayerChallenge(
    DateOnly Day,
    Dictionary<DailyTaskType, bool> Completion,
    Dictionary<WorldId, Dictionary<DailyTaskType, DailyTaskValues>> Values)
{
    public static PlayerChallenge Empty => new(DateOnly.MinValue, new Dictionary<DailyTaskType, bool>(), new Dictionary<WorldId, Dictionary<DailyTaskType, DailyTaskValues>>());

    internal IReadOnlyDictionary<DailyTaskType, DailyTaskValues> CreateValues(
        World world,
        IReadOnlyDictionary<DailyTaskType, int> targets)
    {
        return DailyChallenges.Value[Day.DayOfWeek].ToDictionary(
            type => type,
            type => new DailyTaskValues(
                type is DailyTaskType.PostViews or DailyTaskType.PostLikes
                    ? (long)Math.Ceiling((decimal)world.Player.Subscribers * targets[type] / 100)
                    : targets[type],
                0,
                type == DailyTaskType.StarComment && world.Stars.Count > 0 ? world.Stars[Random.Shared.Next(world.Stars.Count)].Id : null));
    }
}

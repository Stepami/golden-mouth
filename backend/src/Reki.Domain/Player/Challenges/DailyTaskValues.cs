using Reki.Domain.Worlds;

namespace Reki.Domain.Player.Challenges;

public sealed record DailyTaskValues(
    long BaseTarget,
    long Target,
    long Progress,
    StarId? StarId)
{
    internal static Dictionary<WorldId, Dictionary<DailyTaskType, DailyTaskValues>> Create(
        IReadOnlyCollection<World> worlds,
        IReadOnlyDictionary<DailyTaskType, long> targets) =>
        worlds.ToDictionary(
            world => world.Id,
            world => CreateForWorld(world, targets));

    internal static Dictionary<DailyTaskType, DailyTaskValues> CreateForWorld(
        World world,
        IReadOnlyDictionary<DailyTaskType, long> targets)
    {
        var starId = world.Stars[Random.Shared.Next(world.Stars.Count)].Id;
        return targets.ToDictionary(
            target => target.Key,
            target =>
            {
                var targetValue = target.Key is DailyTaskType.PostViews or DailyTaskType.PostLikes
                    ? (long)Math.Ceiling((decimal)world.Player.Subscribers * targets[target.Key] / 100)
                    : targets[target.Key];
                return new DailyTaskValues(
                    targets[target.Key],
                    targetValue,
                    0,
                    target.Key == DailyTaskType.StarComment ? starId : null);
            });
    }
}

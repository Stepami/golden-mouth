using Reki.Domain.Worlds;

namespace Reki.Domain.Player.Challenges;

public sealed record PlayerChallenge(
    DateOnly Day,
    Dictionary<DailyTaskType, bool> Completion,
    Dictionary<WorldId, Dictionary<DailyTaskType, DailyTaskValues>> Values)
{
    public static PlayerChallenge Empty => new(DateOnly.MinValue, [], []);

    public static PlayerChallenge New(
        DateOnly day,
        IReadOnlyCollection<World> worlds,
        IReadOnlyDictionary<DailyTaskType, long> targets)
    {
        var completion = targets.ToDictionary(kvp => kvp.Key, _ => false);
        return new PlayerChallenge(
            day,
            completion,
            DailyTaskValues.Create(worlds, targets));
    }

    internal void AddWorld(
        World world,
        IReadOnlyDictionary<DailyTaskType, long> targets)
    {
        if (Values.Count == 0)
        {
            Values[world.Id] = DailyTaskValues.CreateForWorld(world, targets);
        }
        else
        {
            var existingTargets = Values[Values.Keys.First()].ToDictionary(
                target => target.Key,
                target => target.Value.BaseTarget);
            Values[world.Id] = DailyTaskValues.CreateForWorld(world, existingTargets);
        }
    }

    internal bool RemoveWorld(WorldId worldId) => Values.Remove(worldId);
}

using Reki.Domain.Worlds;

namespace Reki.Domain.Player.Challenges;

public sealed record PlayerChallenge(
    DateOnly Day,
    Dictionary<DailyTaskType, bool> Completion,
    Dictionary<WorldId, Dictionary<DailyTaskType, DailyTaskValues>> Values)
{
    public static PlayerChallenge Empty => new(DateOnly.MinValue, [], []);

    internal bool IsCompleted => Completion.Values.All(done => done);

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

    internal void ApplyProgress(
        WorldId worldId,
        IReadOnlyDictionary<DailyTaskType, long> changes,
        StarId? starId)
    {
        if (!Values.TryGetValue(worldId, out var tasks))
        {
            return;
        }

        foreach (var (type, value) in changes)
        {
            if (!Completion.ContainsKey(type) ||
                !tasks.TryGetValue(type, out var task) ||
                (type == DailyTaskType.StarComment && (starId is null || task.StarId != starId)))
            {
                continue;
            }

            var progress = type switch
            {
                DailyTaskType.PostViews or DailyTaskType.PostLikes => Math.Max(task.Progress, value),
                DailyTaskType.StarComment => 1,
                _ => task.Progress + value,
            };

            task.UpdateProgress(progress);
            if (progress >= task.Target)
            {
                Completion[type] = true;
            }
        }
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

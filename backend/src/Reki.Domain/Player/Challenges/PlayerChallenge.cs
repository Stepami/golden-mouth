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

    internal bool ApplyProgress(
        WorldId worldId,
        IReadOnlyList<(DailyTaskType Type, long Value)> changes,
        StarId? starId)
    {
        if (!Values.TryGetValue(worldId, out var tasks))
        {
            return false;
        }

        var changed = false;
        foreach (var (type, value) in changes)
        {
            if (!Completion.TryGetValue(type, out var completed) ||
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

            if (progress != task.Progress)
            {
                tasks[type] = task with { Progress = progress };
                changed = true;
            }

            if (!completed && progress >= task.Target)
            {
                Completion[type] = true;
                changed = true;
            }
        }

        return changed;
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

namespace Reki.Domain.Player.Challenges;

public static class DailyChallenges
{
    public static readonly IReadOnlyDictionary<DayOfWeek, IReadOnlyList<DailyTaskType>> Value =
        new Dictionary<DayOfWeek, IReadOnlyList<DailyTaskType>>
        {
            [DayOfWeek.Monday] = [DailyTaskType.Posts, DailyTaskType.Comments, DailyTaskType.Subscribers],
            [DayOfWeek.Tuesday] = [DailyTaskType.Posts, DailyTaskType.Comments, DailyTaskType.StarComment],
            [DayOfWeek.Wednesday] = [DailyTaskType.Reposts, DailyTaskType.Comments, DailyTaskType.PostViews],
            [DayOfWeek.Thursday] = [DailyTaskType.Posts, DailyTaskType.PostLikes, DailyTaskType.Subscribers],
            [DayOfWeek.Friday] = [DailyTaskType.Posts, DailyTaskType.Reposts, DailyTaskType.Unsubscribers],
            [DayOfWeek.Saturday] = [DailyTaskType.Comments, DailyTaskType.StarComment, DailyTaskType.PostViews],
            [DayOfWeek.Sunday] = [DailyTaskType.Posts, DailyTaskType.Likes, DailyTaskType.PostLikes],
        };
}

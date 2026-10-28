namespace Reki.Application.Posts;

public interface IStarFeedScheduler<in TSchedulerRequest>
{
    ValueTask ScheduleFeedAsync(TSchedulerRequest request);
}

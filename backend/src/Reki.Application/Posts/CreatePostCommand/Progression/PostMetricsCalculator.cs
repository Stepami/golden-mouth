using Reki.Application.Balance;

namespace Reki.Application.Posts.CreatePostCommand.Progression;

internal static class PostMetricsCalculator
{
    public static (long Views, long Likes) Calculate(
        long subscribers,
        bool isRepost,
        bool viral,
        GameBalance balance,
        int viralCardMultiplier = 1,
        int flopGuardViewsAdder = 0)
    {
        var views = Random.Shared.NextInt64(subscribers + 1);
        var percentage = Random.Shared.Next(balance.LikesMinPercent, balance.LikesMaxPercent + 1);
        var likes = (long)Math.Ceiling((decimal)views * percentage / 100);

        if (isRepost)
        {
            return (views, likes);
        }

        return viral
            ? (views * 2 * viralCardMultiplier, likes * 6 * viralCardMultiplier)
            : (views + flopGuardViewsAdder, likes);
    }
}
using Microsoft.Extensions.Options;
using Reki.Application.Balance;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.CreatePostCommand.Progression;

internal sealed class PostNumbersCalculator(IOptionsMonitor<GameBalance> balance)
{
    public Result Calculate(World world, ViralityCalculator.Result viralityResult)
    {
        var currentBalance = balance.CurrentValue;
        var percentage = Random.Shared.Next(currentBalance.LikesMinPercent, currentBalance.LikesMaxPercent + 1);

        var baseViews = Random.Shared.NextInt64(world.Player.Subscribers + 1);
        var views = (baseViews * viralityResult.Multiplier) + viralityResult.ViewsAdder;
        var likes =
            (long)Math.Ceiling((decimal)views * percentage / 100) *
            (viralityResult.Virality is PostViralityStatus.Viral or PostViralityStatus.ViralCard ? 3 : 1);

        return new Result(views, likes, baseViews);
    }

    internal sealed record Result(long Views, long Likes, long BaseViews);
}

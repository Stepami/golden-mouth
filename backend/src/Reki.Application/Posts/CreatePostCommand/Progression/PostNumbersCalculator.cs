using Microsoft.Extensions.Options;
using Reki.Application.Balance;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.CreatePostCommand.Progression;

internal sealed class PostNumbersCalculator(IOptionsMonitor<GameBalance> balance)
{
    public Result Calculate(World world, bool viral)
    {
        var currentBalance = balance.CurrentValue;
        var percentage = Random.Shared.Next(currentBalance.LikesMinPercent, currentBalance.LikesMaxPercent + 1);

        var views =
            Random.Shared.NextInt64(world.Player.Subscribers + 1) *
            (viral ? 2 : 1);
        var likes =
            (long)Math.Ceiling((decimal)views * percentage / 100) *
            (viral ? 3 : 1);

        return new Result(views, likes);
    }

    internal sealed record Result(long Views, long Likes);
}
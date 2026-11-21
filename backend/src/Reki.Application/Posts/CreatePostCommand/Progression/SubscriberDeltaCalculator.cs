using Microsoft.Extensions.Options;
using Reki.Application.Balance;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.CreatePostCommand.Progression;

internal sealed class SubscriberDeltaCalculator(IOptionsMonitor<GameBalance> balance)
{
    public long GetDelta(
        World world,
        GamePost post,
        PostScore turn,
        bool ownWords,
        bool viral,
        bool flop)
    {
        if (post.RepostId is not null)
        {
            return 1L;
        }

        var currentBalance = balance.CurrentValue;
        var worldDifficulty = currentBalance.Difficulties[world.Difficulty];

        var multiplier = GetGrowthMultiplier(world, ownWords);
        var growth =
            currentBalance.Growth.Coefficient * worldDifficulty.GrowthCoefficient *
            (0.5 + turn.Quality) *
            Math.Pow(Math.Max(1, world.Player.Subscribers), currentBalance.Growth.Power) *
            multiplier *
            (flop ? worldDifficulty.Drama[world.Player.Drama].LossMultiplier : 1);

        var viralMultiplier = viral
            ? currentBalance.Viral.Multiplier * worldDifficulty.ViralMultiplier
            : 1;

        var delta = Math.Max(1L, (long)Math.Round(growth * viralMultiplier));
        return flop ? -Math.Min(world.Player.Subscribers, delta) : delta;
    }

    private double GetGrowthMultiplier(World gameWorld, bool ownWords)
    {
        var currentBalance = balance.CurrentValue;
        double multiplier = 1;
        if (gameWorld.Player.Subscribers < currentBalance.Growth.StarterBelowSubscribers)
        {
            multiplier *= currentBalance.Growth.StarterMultiplier;
        }

        if (ownWords)
        {
            multiplier *= currentBalance.Growth.OwnWordsMultiplier;
        }

        return multiplier;
    }
}
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Reki.Application.Balance;
using Reki.Application.Player;
using Reki.Domain.Bonuses;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.CreatePostCommand.Progression;

internal sealed class ViralityCalculator(IOptionsMonitor<GameBalance> balance, IBonusRepository bonuses)
{
    public async Task<Result> CalculateAsync(
        GamePost post,
        PostScore turn,
        World world,
        CancellationToken cancellationToken)
    {
        if (post.RepostId is not null)
        {
            return new(PostViralityStatus.None);
        }

        if (turn.IsFlop)
        {
            var guard = await bonuses.TryClaimBonus<FlopGuardBonus>(post.OwnerId, cancellationToken);
            return guard is null
                ? new(PostViralityStatus.Flop)
                : new(PostViralityStatus.FlopGuard, ViewsAdder: guard.Adder);
        }

        if (IsNaturallyViral(post, turn, world))
        {
            return new(
                PostViralityStatus.Viral,
                Multiplier: balance.CurrentValue.Difficulties[world.Difficulty].ViralMultiplier);
        }

        var card = await bonuses.TryClaimBonus<ViralCardBonus>(post.OwnerId, cancellationToken);
        return card is null
            ? new(PostViralityStatus.None)
            : new(
                PostViralityStatus.ViralCard,
                card.Multiplier * balance.CurrentValue.Viral.Multiplier);
    }

    private static double StableRoll(GamePost post)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{post.Id}:viral"));
        var value = BinaryPrimitives.ReadUInt64LittleEndian(hash);
        return value / (double)ulong.MaxValue;
    }

    private bool IsNaturallyViral(GamePost post, PostScore turn, World world)
    {
        var currentBalance = balance.CurrentValue;
        var chance =
            (currentBalance.Viral.BaseChance + (currentBalance.Viral.ChancePerQuality * turn.Quality) +
             (turn.PositiveEmotion ? currentBalance.Viral.EmotionBonus : 0)) *
            currentBalance.Difficulties[world.Difficulty].ViralMultiplier;
        return StableRoll(post) < Math.Clamp(chance, 0, 1);
    }

    internal sealed record Result(PostViralityStatus Virality, int Multiplier = 1, int ViewsAdder = 0);
}

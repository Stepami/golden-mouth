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
    internal enum Outcome
    {
        Regular,
        Viral,
        Flop,
        GuardedFlop,
    }

    public async Task<Result> CalculateAsync(
        GamePost post,
        PostScore turn,
        World world,
        CancellationToken cancellationToken)
    {
        if (post.RepostId is not null)
        {
            return new(Outcome.Regular);
        }

        if (turn.IsFlop)
        {
            var guard = await bonuses.TryClaimBonus<FlopGuardBonus>(post.OwnerId, cancellationToken);
            return guard is null ? new(Outcome.Flop) : new(Outcome.GuardedFlop, ViewsAdder: guard.Adder);
        }

        if (IsNaturallyViral(post, turn, world))
        {
            return new(Outcome.Viral);
        }

        var card = await bonuses.TryClaimBonus<ViralCardBonus>(post.OwnerId, cancellationToken);
        return card is null ? new(Outcome.Regular) : new(Outcome.Viral, card.Multiplier);
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

    internal sealed record Result(Outcome Kind, int Multiplier = 1, int ViewsAdder = 0);
}
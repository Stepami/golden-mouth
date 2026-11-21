using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Reki.Application.Balance;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.CreatePostCommand.Progression;

internal sealed class ViralityCalculator(IOptionsMonitor<GameBalance> balance)
{
    public Result IsViral(
        GamePost post,
        PostScore turn,
        World world)
    {
        if (post.RepostId is not null)
        {
            return new(false, false);
        }

        if (turn.IsFlop)
        {
            return new(false, turn.IsFlop);
        }

        var currentBalance = balance.CurrentValue;
        var chance =
            (currentBalance.Viral.BaseChance + (currentBalance.Viral.ChancePerQuality * turn.Quality) +
             (turn.PositiveEmotion ? currentBalance.Viral.EmotionBonus : 0)) *
            currentBalance.Difficulties[world.Difficulty].ViralMultiplier;
        var viral = StableRoll(post) < Math.Clamp(chance, 0, 1);
        return new(viral, false);
    }

    private static double StableRoll(GamePost post)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{post.Id}:viral"));
        var value = BinaryPrimitives.ReadUInt64LittleEndian(hash);
        return value / (double)ulong.MaxValue;
    }

    internal sealed record Result(bool Viral, bool Flop);
}
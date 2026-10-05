using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Reki.Application.Common;
using Reki.Domain.Player;

namespace Reki.Application.Posts;

public sealed class PostProgressionCalculator(GameBalance balance)
{
    private static readonly SkillId Brave = SkillId.From("brave");
    private static readonly SkillId Humor = SkillId.From("humor");
    private static readonly SkillId Topic = SkillId.From("topic");

    public PostProgression Calculate(
        PlayerProfile player,
        GeneratedPostTurn turn,
        bool ownWords,
        bool wantCompleted)
    {
        WorldBalance world = balance.Worlds.TryGetValue(player.World.Value, out WorldBalance? value)
            ? value
            : throw new InvalidOperationException($"World balance is missing for '{player.World.Value}'.");
        double quality = Math.Clamp((turn.Brave + turn.Humor + turn.Topic) / 3, 0, 1);
        int postIndex = player.PostsUsed + 1;
        double multiplier = GetGrowthMultiplier(player, postIndex, ownWords);
        bool viral = IsViral(player, quality, world);
        bool flop = !viral && IsFlop(player, world);
        if (viral)
        {
            multiplier *= balance.Viral.Multiplier * world.ViralMultiplier;
        }

        double decay = balance.Growth.Decay
            .First(item => postIndex >= item.From && postIndex <= item.To)
            .Multiplier;
        double growth = balance.Growth.Coefficient * world.GrowthCoefficient *
            (0.5 + quality) *
            Math.Pow(Math.Max(1, player.Subscribers), balance.Growth.Power) *
            decay *
            multiplier;
        long subscriberDelta = flop
            ? Math.Min(-1, (long)Math.Round(growth * balance.Flop.LossMultiplier))
            : Math.Max(1, (long)Math.Round(growth));
        IReadOnlyDictionary<SkillId, SkillProgress> skills = AddSkillExperience(
            player,
            turn,
            postIndex,
            ownWords,
            wantCompleted);
        return new PostProgression(subscriberDelta, quality, viral, flop, skills);
    }

    private static SkillId GetMainSkill(GeneratedPostTurn turn)
    {
        if (turn.Brave >= turn.Humor && turn.Brave >= turn.Topic)
        {
            return Brave;
        }

        return turn.Humor >= turn.Topic ? Humor : Topic;
    }

    private static double StableRoll(PlayerProfile player, int postNumber, string purpose)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{player.Id.Value}:{postNumber}:{purpose}"));
        ulong value = BinaryPrimitives.ReadUInt64LittleEndian(hash);
        return value / (double)ulong.MaxValue;
    }

    private double GetGrowthMultiplier(PlayerProfile player, int postIndex, bool ownWords)
    {
        double multiplier = 1;
        if (player.Subscribers < balance.Growth.StarterBelowSubscribers)
        {
            multiplier *= balance.Growth.StarterMultiplier;
        }

        if (ownWords)
        {
            multiplier *= balance.Growth.OwnWordsMultiplier;
        }

        if (player.RestedPostsRemaining > 0 && postIndex <= balance.Growth.RestedFirstPosts)
        {
            multiplier *= balance.Growth.RestedMultiplier;
        }

        return multiplier;
    }

    private bool IsViral(PlayerProfile player, double quality, WorldBalance world)
    {
        int nextPost = player.TotalPostsPublished + 1;
        bool hasNeverGoneViral = player.TotalPostsPublished == player.PostsSinceViral;
        if ((hasNeverGoneViral && nextPost == balance.Viral.GuaranteedPost) ||
            player.PostsSinceViral + 1 >= balance.Viral.PityPosts)
        {
            return true;
        }

        double chance = (balance.Viral.BaseChance + (balance.Viral.ChancePerQuality * quality)) *
            world.ViralMultiplier;
        return StableRoll(player, nextPost, "viral") < Math.Clamp(chance, 0, 1);
    }

    private bool IsFlop(PlayerProfile player, WorldBalance world)
    {
        if (player.ConsecutiveFlops >= balance.Flop.MaxInRow)
        {
            return false;
        }

        return StableRoll(player, player.TotalPostsPublished + 1, "flop") < world.FlopChance;
    }

    private IReadOnlyDictionary<SkillId, SkillProgress> AddSkillExperience(
        PlayerProfile player,
        GeneratedPostTurn turn,
        int postIndex,
        bool ownWords,
        bool wantCompleted)
    {
        if (postIndex > balance.Skills.ExperiencePostLimit)
        {
            return player.Skills;
        }

        SkillId main = GetMainSkill(turn);
        double multiplier = postIndex <= balance.Skills.FirstPostsCount
            ? balance.Skills.FirstPostsMultiplier
            : 1;
        if (ownWords)
        {
            multiplier *= balance.Skills.OwnWordsMultiplier;
        }

        if (wantCompleted)
        {
            multiplier *= balance.Skills.WantDoneMultiplier;
        }

        return player.Skills.ToDictionary(
            item => item.Key,
            item => item.Value.AddExperience(
                (item.Key == main ? balance.Skills.MainExperience : balance.Skills.SideExperience) *
                    multiplier,
                balance.Skills.LevelThresholds));
    }
}

using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Reki.Application.Balance;
using Reki.Application.Posts.AI;
using Reki.Domain.Player;

namespace Reki.Application.Posts.PublishPostCommand;

internal static class PostProgressionCalculator
{
    private static readonly SkillId Brave = SkillId.From("brave");
    private static readonly SkillId Humor = SkillId.From("humor");
    private static readonly SkillId Topic = SkillId.From("topic");

    public static PostProgression Calculate(
        PlayerProfile player,
        Domain.Worlds.World gameWorld,
        GeneratedPostTurn turn,
        bool ownWords,
        bool wantCompleted,
        string publicationId,
        GameBalance currentBalance)
    {
        var world = currentBalance.Difficulties[gameWorld.Difficulty];
        var quality = Math.Clamp((turn.Brave + turn.Humor + turn.Topic) / 3, 0, 1);
        var postIndex = player.EnergyUsed + 1;
        var multiplier = GetGrowthMultiplier(gameWorld, postIndex, ownWords, currentBalance);
        var viral = IsViral(publicationId, quality, world, currentBalance);
        var flop = !viral && IsFlop(publicationId, world);
        if (viral)
        {
            multiplier *= currentBalance.Viral.Multiplier * world.ViralMultiplier;
        }

        var decay = currentBalance.Growth.Decay
            .First(item => postIndex >= item.From && postIndex <= item.To)
            .Multiplier;
        var growth = currentBalance.Growth.Coefficient * world.GrowthCoefficient *
                     (0.5 + quality) *
                     Math.Pow(Math.Max(1, gameWorld.Player.Subscribers), currentBalance.Growth.Power) *
                     decay *
                     multiplier;
        var subscriberDelta = flop
            ? Math.Min(-1, (long)Math.Round(growth * currentBalance.Flop.LossMultiplier))
            : Math.Max(1, (long)Math.Round(growth));
        var skills = AddSkillExperience(
            player,
            turn,
            postIndex,
            ownWords,
            wantCompleted,
            currentBalance);
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

    private static double StableRoll(string publicationId, string purpose)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{publicationId}:{purpose}"));
        var value = BinaryPrimitives.ReadUInt64LittleEndian(hash);
        return value / (double)ulong.MaxValue;
    }

    private static bool IsFlop(string publicationId, DifficultyBalance world) =>
        StableRoll(publicationId, "flop") < world.FlopChance;

    private static double GetGrowthMultiplier(Domain.Worlds.World gameWorld, int postIndex, bool ownWords, GameBalance currentBalance)
    {
        double multiplier = 1;
        if (gameWorld.Player.Subscribers < currentBalance.Growth.StarterBelowSubscribers)
        {
            multiplier *= currentBalance.Growth.StarterMultiplier;
        }

        if (ownWords)
        {
            multiplier *= currentBalance.Growth.OwnWordsMultiplier;
        }

        if (gameWorld.Player.RestedPostsRemaining > 0 && postIndex <= currentBalance.Growth.RestedFirstPosts)
        {
            multiplier *= currentBalance.Growth.RestedMultiplier;
        }

        return multiplier;
    }

    private static bool IsViral(string publicationId, double quality, DifficultyBalance world, GameBalance currentBalance)
    {
        var chance = (currentBalance.Viral.BaseChance + (currentBalance.Viral.ChancePerQuality * quality)) *
                     world.ViralMultiplier;
        return StableRoll(publicationId, "viral") < Math.Clamp(chance, 0, 1);
    }

    private static IReadOnlyDictionary<SkillId, SkillProgress> AddSkillExperience(
        PlayerProfile player,
        GeneratedPostTurn turn,
        int postIndex,
        bool ownWords,
        bool wantCompleted,
        GameBalance currentBalance)
    {
        if (postIndex > currentBalance.Skills.ExperiencePostLimit)
        {
            return player.Skills;
        }

        var main = GetMainSkill(turn);
        var multiplier = postIndex <= currentBalance.Skills.FirstPostsCount
            ? currentBalance.Skills.FirstPostsMultiplier
            : 1;
        if (ownWords)
        {
            multiplier *= currentBalance.Skills.OwnWordsMultiplier;
        }

        if (wantCompleted)
        {
            multiplier *= currentBalance.Skills.WantDoneMultiplier;
        }

        return player.Skills.ToDictionary(
            item => item.Key,
            item => item.Value.AddExperience(
                (item.Key == main ? currentBalance.Skills.MainExperience : currentBalance.Skills.SideExperience) *
                    multiplier,
                currentBalance.Skills.LevelThresholds));
    }
}

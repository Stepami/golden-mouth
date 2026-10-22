using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Reki.Application.Common.Balance;
using Reki.Domain.Player;

namespace Reki.Application.Posts.PublishPostCommand;

public sealed class PostProgressionCalculator(IOptions<GameBalance> balance)
{
    private static readonly SkillId Brave = SkillId.From("brave");
    private static readonly SkillId Humor = SkillId.From("humor");
    private static readonly SkillId Topic = SkillId.From("topic");

    public PostProgression Calculate(
        PlayerProfile player,
        Reki.Domain.Worlds.World gameWorld,
        GeneratedPostTurn turn,
        bool ownWords,
        bool wantCompleted,
        string publicationId)
    {
        var world = balance.Value.Worlds.TryGetValue(gameWorld.BalanceKey, out var value)
            ? value
            : throw new InvalidOperationException($"World balance is missing for '{gameWorld.BalanceKey}'.");
        var quality = Math.Clamp((turn.Brave + turn.Humor + turn.Topic) / 3, 0, 1);
        var postIndex = player.EnergyUsed + 1;
        var multiplier = GetGrowthMultiplier(gameWorld, postIndex, ownWords);
        var viral = IsViral(publicationId, quality, world);
        var flop = !viral && IsFlop(publicationId, world);
        if (viral)
        {
            multiplier *= balance.Value.Viral.Multiplier * world.ViralMultiplier;
        }

        var decay = balance.Value.Growth.Decay
            .First(item => postIndex >= item.From && postIndex <= item.To)
            .Multiplier;
        var growth = balance.Value.Growth.Coefficient * world.GrowthCoefficient *
                     (0.5 + quality) *
                     Math.Pow(Math.Max(1, gameWorld.Player.Subscribers), balance.Value.Growth.Power) *
                     decay *
                     multiplier;
        var subscriberDelta = flop
            ? Math.Min(-1, (long)Math.Round(growth * balance.Value.Flop.LossMultiplier))
            : Math.Max(1, (long)Math.Round(growth));
        var skills = AddSkillExperience(
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

    private static double StableRoll(string publicationId, string purpose)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{publicationId}:{purpose}"));
        var value = BinaryPrimitives.ReadUInt64LittleEndian(hash);
        return value / (double)ulong.MaxValue;
    }

    private static bool IsFlop(string publicationId, WorldBalance world) =>
        StableRoll(publicationId, "flop") < world.FlopChance;

    private double GetGrowthMultiplier(Reki.Domain.Worlds.World gameWorld, int postIndex, bool ownWords)
    {
        double multiplier = 1;
        if (gameWorld.Player.Subscribers < balance.Value.Growth.StarterBelowSubscribers)
        {
            multiplier *= balance.Value.Growth.StarterMultiplier;
        }

        if (ownWords)
        {
            multiplier *= balance.Value.Growth.OwnWordsMultiplier;
        }

        if (gameWorld.Player.RestedPostsRemaining > 0 && postIndex <= balance.Value.Growth.RestedFirstPosts)
        {
            multiplier *= balance.Value.Growth.RestedMultiplier;
        }

        return multiplier;
    }

    private bool IsViral(string publicationId, double quality, WorldBalance world)
    {
        var chance = (balance.Value.Viral.BaseChance + (balance.Value.Viral.ChancePerQuality * quality)) *
                     world.ViralMultiplier;
        return StableRoll(publicationId, "viral") < Math.Clamp(chance, 0, 1);
    }

    private IReadOnlyDictionary<SkillId, SkillProgress> AddSkillExperience(
        PlayerProfile player,
        GeneratedPostTurn turn,
        int postIndex,
        bool ownWords,
        bool wantCompleted)
    {
        if (postIndex > balance.Value.Skills.ExperiencePostLimit)
        {
            return player.Skills;
        }

        var main = GetMainSkill(turn);
        var multiplier = postIndex <= balance.Value.Skills.FirstPostsCount
            ? balance.Value.Skills.FirstPostsMultiplier
            : 1;
        if (ownWords)
        {
            multiplier *= balance.Value.Skills.OwnWordsMultiplier;
        }

        if (wantCompleted)
        {
            multiplier *= balance.Value.Skills.WantDoneMultiplier;
        }

        return player.Skills.ToDictionary(
            item => item.Key,
            item => item.Value.AddExperience(
                (item.Key == main ? balance.Value.Skills.MainExperience : balance.Value.Skills.SideExperience) *
                    multiplier,
                balance.Value.Skills.LevelThresholds));
    }
}
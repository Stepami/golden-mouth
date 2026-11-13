using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Reki.Application.Balance;
using Reki.Application.Posts.CreatePostCommand.Score;
using Reki.Domain.Player;

namespace Reki.Application.Posts.CreatePostCommand.Progression;

internal sealed class PostProgressionCalculator(IOptionsMonitor<GameBalance> balance)
{
    private static readonly SkillId Brave = SkillId.From("brave");
    private static readonly SkillId Humor = SkillId.From("humor");
    private static readonly SkillId Topic = SkillId.From("topic");

    public PostProgression Calculate(
        PlayerProfile player,
        Domain.Worlds.World gameWorld,
        PostScore turn,
        bool ownWords,
        string publicationId,
        bool flop,
        bool repost = false)
    {
        var currentBalance = balance.CurrentValue;
        var world = currentBalance.Difficulties[gameWorld.Difficulty];
        var quality = Math.Clamp((turn.Brave + turn.Humor + turn.Topic) / 3, 0, 1);
        if (repost)
        {
            return new PostProgression(1, quality, false, false, player.Skills);
        }

        var multiplier = GetGrowthMultiplier(gameWorld, ownWords);
        var viral = !flop && IsViral(publicationId, quality, turn.PositiveEmotion, world);

        var growth = currentBalance.Growth.Coefficient * world.GrowthCoefficient *
                     (0.5 + quality) *
                     Math.Pow(Math.Max(1, gameWorld.Player.Subscribers), currentBalance.Growth.Power) *
                     multiplier;
        var drama =
            (world.Drama ?? throw new InvalidOperationException("Drama balance is missing."))[gameWorld.Player.Drama];
        var viralMultiplier = viral ? currentBalance.Viral.Multiplier * world.ViralMultiplier : 1;
        var subscriberDelta = flop
            ? -Math.Min(gameWorld.Player.Subscribers, Math.Min(
                Math.Max(1, (long)Math.Round(growth * world.LossMultiplier * drama.LossMultiplier)),
                Math.Max(1, (long)Math.Floor(gameWorld.Player.Subscribers * world.MaxSubscriberFraction * drama.LossMultiplier))))
            : Math.Max(1, (long)Math.Round(growth * viralMultiplier));
        var skills = AddSkillExperience(
            player,
            turn,
            ownWords);
        return new PostProgression(subscriberDelta, quality, viral, flop, skills);
    }

    private static SkillId GetMainSkill(PostScore turn)
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

    private double GetGrowthMultiplier(Domain.Worlds.World gameWorld, bool ownWords)
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

    private bool IsViral(string publicationId, double quality, bool positiveEmotion, DifficultyBalance world)
    {
        var currentBalance = balance.CurrentValue;
        var chance = (currentBalance.Viral.BaseChance + (currentBalance.Viral.ChancePerQuality * quality) +
                      (positiveEmotion ? currentBalance.Viral.EmotionBonus : 0)) *
                     world.ViralMultiplier;
        return StableRoll(publicationId, "viral") < Math.Clamp(chance, 0, 1);
    }

    private IReadOnlyDictionary<SkillId, SkillProgress> AddSkillExperience(
        PlayerProfile player,
        PostScore turn,
        bool ownWords)
    {
        var currentBalance = balance.CurrentValue;
        var main = GetMainSkill(turn);
        double multiplier = 1;
        if (ownWords)
        {
            multiplier *= currentBalance.Skills.OwnWordsMultiplier;
        }

        return player.Skills.ToDictionary(
            item => item.Key,
            item => item.Value.AddExperience(
                (item.Key == main ? currentBalance.Skills.MainExperience : currentBalance.Skills.SideExperience) *
                multiplier,
                currentBalance.Skills.LevelThresholds));
    }
}

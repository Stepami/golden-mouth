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
        bool wantCompleted,
        string publicationId,
        bool flop)
    {
        var currentBalance = balance.CurrentValue;
        var world = currentBalance.Difficulties[gameWorld.Difficulty];
        var quality = Math.Clamp((turn.Brave + turn.Humor + turn.Topic) / 3, 0, 1);
        var postIndex = player.EnergyUsed + 1;
        var multiplier = GetGrowthMultiplier(gameWorld, postIndex, ownWords);
        var viral = !flop && IsViral(publicationId, quality, world);

        var decay = currentBalance.Growth.Decay
            .First(item => postIndex >= item.From && postIndex <= item.To)
            .Multiplier;
        var growth = currentBalance.Growth.Coefficient * world.GrowthCoefficient *
                     (0.5 + quality) *
                     Math.Pow(Math.Max(1, gameWorld.Player.Subscribers), currentBalance.Growth.Power) *
                     decay *
                     multiplier;
        var drama = (world.Drama ?? throw new InvalidOperationException("Drama balance is missing."))[gameWorld.Player.Drama];
        var viralMultiplier = viral ? currentBalance.Viral.Multiplier * world.ViralMultiplier : 1;
        var subscriberDelta = flop
            ? -Math.Min(gameWorld.Player.Subscribers, Math.Min(
                Math.Max(1, (long)Math.Round(growth * world.LossMultiplier * drama.LossMultiplier)),
                Math.Max(1, (long)Math.Floor(gameWorld.Player.Subscribers * world.MaxSubscriberFraction * drama.LossMultiplier))))
            : Math.Max(1, (long)Math.Round(growth * viralMultiplier));
        var skills = AddSkillExperience(
            player,
            turn,
            postIndex,
            ownWords,
            wantCompleted);
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

    private double GetGrowthMultiplier(Domain.Worlds.World gameWorld, int postIndex, bool ownWords)
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

        if (gameWorld.Player.RestedPostsRemaining > 0 && postIndex <= currentBalance.Growth.RestedFirstPosts)
        {
            multiplier *= currentBalance.Growth.RestedMultiplier;
        }

        return multiplier;
    }

    private bool IsViral(string publicationId, double quality, DifficultyBalance world)
    {
        var currentBalance = balance.CurrentValue;
        var chance = (currentBalance.Viral.BaseChance + (currentBalance.Viral.ChancePerQuality * quality)) *
                     world.ViralMultiplier;
        return StableRoll(publicationId, "viral") < Math.Clamp(chance, 0, 1);
    }

    private IReadOnlyDictionary<SkillId, SkillProgress> AddSkillExperience(
        PlayerProfile player,
        PostScore turn,
        int postIndex,
        bool ownWords,
        bool wantCompleted)
    {
        var currentBalance = balance.CurrentValue;
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

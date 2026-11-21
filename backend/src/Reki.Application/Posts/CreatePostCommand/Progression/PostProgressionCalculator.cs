using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Reki.Application.Balance;
using Reki.Application.Posts.CreatePostCommand.Progression.Score;
using Reki.Application.Posts.ML;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.CreatePostCommand.Progression;

internal sealed class PostProgressionCalculator(
    PostScorer scorer,
    IStarVectorStore stars,
    IOptionsMonitor<GameBalance> balance)
{
    public async Task<PostProgression> CalculateAsync(
        PlayerProfile player,
        World gameWorld,
        GamePost post,
        bool ownWords)
    {
        var turn = await scorer.ScoreAsync(
            post,
            gameWorld,
            ownWords);
        var starId = await stars.GetRelevantCommenterAsync(gameWorld, post);

        var currentBalance = balance.CurrentValue;
        var world = currentBalance.Difficulties[gameWorld.Difficulty];
        var quality = Math.Clamp((turn.Brave + turn.Humor + turn.Topic) / 3, 0, 1);

        var isRepost = post.RepostId is not null;
        var flop = !isRepost && turn.FlopReason is not null;
        var viral = !isRepost && !flop && IsViral(post.Id.ToString(), quality, turn.PositiveEmotion, world);
        var skills = player.Skills;
        long subscriberDelta = 1;
        if (!isRepost)
        {
            var multiplier = GetGrowthMultiplier(gameWorld, ownWords);
            var growth = currentBalance.Growth.Coefficient * world.GrowthCoefficient *
                         (0.5 + quality) *
                         Math.Pow(Math.Max(1, gameWorld.Player.Subscribers), currentBalance.Growth.Power) *
                         multiplier;
            var drama =
                (world.Drama ?? throw new InvalidOperationException("Drama balance is missing."))[
                    gameWorld.Player.Drama];
            var viralMultiplier = viral ? currentBalance.Viral.Multiplier * world.ViralMultiplier : 1;
            subscriberDelta = flop
                ? -Math.Min(
                    gameWorld.Player.Subscribers,
                    Math.Min(
                        Math.Max(1, (long)Math.Round(growth * world.LossMultiplier * drama.LossMultiplier)),
                        Math.Max(
                            1,
                            (long)Math.Floor(gameWorld.Player.Subscribers * world.MaxSubscriberFraction *
                                             drama.LossMultiplier))))
                : Math.Max(1, (long)Math.Round(growth * viralMultiplier));

            skills = AddSkillExperience(player, turn, ownWords);
        }

        var (views, likes) =
            PostMetricsCalculator.Calculate(gameWorld.Player.Subscribers, isRepost, viral, currentBalance);
        return new PostProgression(
            subscriberDelta,
            quality,
            viral,
            flop,
            skills,
            views,
            likes,
            starId,
            turn);
    }

    private static double StableRoll(string publicationId, string purpose)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{publicationId}:{purpose}"));
        var value = BinaryPrimitives.ReadUInt64LittleEndian(hash);
        return value / (double)ulong.MaxValue;
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
        var main = turn.GetMainSkill();
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
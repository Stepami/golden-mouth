using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Reki.Application.Balance;
using Reki.Application.Posts.CreatePostCommand.Progression.Score;
using Reki.Application.Posts.ML;
using Reki.Domain.Bonuses;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.CreatePostCommand.Progression;

internal sealed class PostProgressionCalculator(
    PostScorer scorer,
    IStarVectorStore stars)
{
    public static (double Quality, bool IsRepost, bool Viral, bool Flop) GetOutcome(
        World gameWorld,
        GamePost post,
        PostScore score,
        GameBalance balance)
    {
        var quality = Math.Clamp((score.Brave + score.Humor + score.Topic) / 3, 0, 1);
        var isRepost = post.RepostId is not null;
        var flop = !isRepost && score.FlopReason is not null;
        var viral = !isRepost && !flop && IsViral(post.Id, quality, score.PositiveEmotion, gameWorld, balance);
        return (quality, isRepost, viral, flop);
    }

    public static PostProgression Calculate(
        PlayerProfile player,
        World gameWorld,
        bool ownWords,
        (double Quality, bool IsRepost, bool Viral, bool Flop) outcome,
        PostScore score,
        StarId commenter,
        Bonus? bonus,
        GameBalance balance)
    {
        var viral = outcome.Viral || (!outcome.Flop && bonus is ViralCardBonus or PostBoosterBonus);
        var flopGuard = outcome.Flop && bonus is FlopGuardBonus or PostBoosterBonus;
        var viralCardMultiplier = (bonus as ViralCardBonus)?.Multiplier ?? 1;
        var flopGuardViewsAdder = flopGuard ? (bonus as FlopGuardBonus)?.Adder ?? balance.Bonuses.FlopGuardViews : 0;
        var skills = player.Skills;
        long subscriberDelta = 1;
        if (!outcome.IsRepost)
        {
            var world = balance.Difficulties[gameWorld.Difficulty];
            var multiplier = GetGrowthMultiplier(gameWorld, ownWords, balance);
            var growth = balance.Growth.Coefficient * world.GrowthCoefficient *
                         (0.5 + outcome.Quality) *
                         Math.Pow(Math.Max(1, gameWorld.Player.Subscribers), balance.Growth.Power) *
                         multiplier;
            var drama =
                (world.Drama ?? throw new InvalidOperationException("Drama balance is missing."))[
                    gameWorld.Player.Drama];
            if (outcome.Flop)
            {
                var loss = (long)Math.Round(growth * world.LossMultiplier * drama.LossMultiplier);
                var lossCap = (long)Math.Floor(
                    gameWorld.Player.Subscribers * world.MaxSubscriberFraction * drama.LossMultiplier);
                var lossAmount = Math.Min(gameWorld.Player.Subscribers, Math.Max(1, Math.Min(loss, lossCap)));
                subscriberDelta = flopGuard ? lossAmount : -lossAmount;
            }
            else
            {
                var viralMultiplier = viral ? balance.Viral.Multiplier * world.ViralMultiplier : 1;
                subscriberDelta = Math.Max(1, (long)Math.Round(growth * viralMultiplier)) * viralCardMultiplier;
            }

            skills = AddSkillExperience(player, score, ownWords, balance);
        }

        var (views, likes) = PostMetricsCalculator.Calculate(
            gameWorld.Player.Subscribers, outcome.IsRepost, viral, balance, viralCardMultiplier, flopGuardViewsAdder);
        return new PostProgression(
            subscriberDelta,
            outcome.Quality,
            viral,
            outcome.Flop && !flopGuard,
            skills,
            views,
            likes,
            commenter,
            score);
    }

    public async Task<(PostScore Score, StarId Commenter)> ScoreAsync(
        World gameWorld,
        GamePost post,
        bool ownWords)
    {
        var score = await scorer.ScoreAsync(post, gameWorld, ownWords);
        var commenter = await stars.GetRelevantCommenterAsync(gameWorld, post);
        return (score, commenter);
    }

    private static double StableRoll(string publicationId, string purpose)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{publicationId}:{purpose}"));
        var value = BinaryPrimitives.ReadUInt64LittleEndian(hash);
        return value / (double)ulong.MaxValue;
    }

    private static double GetGrowthMultiplier(World gameWorld, bool ownWords, GameBalance balance)
    {
        double multiplier = 1;
        if (gameWorld.Player.Subscribers < balance.Growth.StarterBelowSubscribers)
        {
            multiplier *= balance.Growth.StarterMultiplier;
        }

        if (ownWords)
        {
            multiplier *= balance.Growth.OwnWordsMultiplier;
        }

        return multiplier;
    }

    private static bool IsViral(PostId postId, double quality, bool positiveEmotion, World gameWorld, GameBalance balance)
    {
        var difficulty = balance.Difficulties[gameWorld.Difficulty];
        var chance = (balance.Viral.BaseChance + (balance.Viral.ChancePerQuality * quality) +
                      (positiveEmotion ? balance.Viral.EmotionBonus : 0)) *
                     difficulty.ViralMultiplier;
        return StableRoll(postId.ToString(), "viral") < Math.Clamp(chance, 0, 1);
    }

    private static IReadOnlyDictionary<SkillId, SkillProgress> AddSkillExperience(
        PlayerProfile player,
        PostScore turn,
        bool ownWords,
        GameBalance balance)
    {
        var main = turn.GetMainSkill();
        double multiplier = 1;
        if (ownWords)
        {
            multiplier *= balance.Skills.OwnWordsMultiplier;
        }

        return player.Skills.ToDictionary(
            item => item.Key,
            item => item.Value.AddExperience(
                (item.Key == main ? balance.Skills.MainExperience : balance.Skills.SideExperience) *
                multiplier,
                balance.Skills.LevelThresholds));
    }
}

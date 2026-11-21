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
        var flop = turn.FlopReason is not null;

        var starId = await stars.GetRelevantCommenterAsync(gameWorld, post);

        var currentBalance = balance.CurrentValue;
        var world = currentBalance.Difficulties[gameWorld.Difficulty];
        var quality = Math.Clamp((turn.Brave + turn.Humor + turn.Topic) / 3, 0, 1);

        var views = Random.Shared.NextInt64(gameWorld.Player.Subscribers + 1);
        var percentage = Random.Shared.Next(currentBalance.LikesMinPercent, currentBalance.LikesMaxPercent + 1);
        var likes = (long)Math.Ceiling((decimal)views * percentage / 100);

        if (post.RepostId is not null)
        {
            return new PostProgression(
                1,
                quality,
                false,
                false,
                player.Skills,
                views,
                likes,
                starId,
                turn);
        }

        var multiplier = GetGrowthMultiplier(gameWorld, ownWords);
        var viral = !flop && IsViral(post.Id.ToString(), quality, turn.PositiveEmotion, world);
        if (viral)
        {
            views *= 2;
            likes *= 2 * 3;
        }

        var growth = currentBalance.Growth.Coefficient * world.GrowthCoefficient *
                     (0.5 + quality) *
                     Math.Pow(Math.Max(1, gameWorld.Player.Subscribers), currentBalance.Growth.Power) *
                     multiplier;
        if (flop)
        {
            growth *= world.Drama[gameWorld.Player.Drama].LossMultiplier;
        }

        var viralMultiplier = viral ? currentBalance.Viral.Multiplier * world.ViralMultiplier : 1;
        var delta = Math.Max(1L, (long)Math.Round(growth * viralMultiplier));
        var subscriberDelta = flop ? -Math.Min(gameWorld.Player.Subscribers, delta) : delta;

        var skills = AddSkillExperience(
            player,
            turn,
            ownWords);
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

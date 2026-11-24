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
    ViralityCalculator viralityCalculator,
    PostNumbersCalculator postNumbersCalculator,
    SubscriberDeltaCalculator subscriberDeltaCalculator,
    IStarVectorStore stars,
    IOptionsMonitor<GameBalance> balance)
{
    public async Task<PostProgression> CalculateAsync(
        PlayerProfile player,
        World gameWorld,
        GamePost post,
        bool ownWords,
        CancellationToken cancellationToken)
    {
        var turn = await scorer.ScoreAsync(
            post,
            gameWorld,
            ownWords);

        var starId = await stars.GetRelevantCommenterAsync(gameWorld, post);
        var result = await viralityCalculator.CalculateAsync(post, turn, gameWorld, cancellationToken);
        var (views, likes) = postNumbersCalculator.Calculate(gameWorld, result);
        var subscriberDelta = subscriberDeltaCalculator.GetDelta(gameWorld, post, turn, ownWords, result);
        var skills = AddSkillExperience(player, post, turn, ownWords);

        return new PostProgression(
            subscriberDelta,
            turn.Quality,
            result.Virality,
            skills,
            views,
            likes,
            starId,
            turn);
    }

    private IReadOnlyDictionary<SkillId, SkillProgress> AddSkillExperience(
        PlayerProfile player,
        GamePost post,
        PostScore turn,
        bool ownWords)
    {
        if (post.RepostId is not null)
        {
            return player.Skills;
        }

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

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
        var viral = result.Kind == ViralityCalculator.Outcome.Viral;
        var flop = result.Kind == ViralityCalculator.Outcome.Flop;
        var guarded = result.Kind == ViralityCalculator.Outcome.GuardedFlop;
        var (views, likes) = postNumbersCalculator.Calculate(gameWorld, viral);
        var subscriberDelta = subscriberDeltaCalculator.GetDelta(gameWorld, post, turn, ownWords, viral, flop || guarded);
        var skills = AddSkillExperience(player, post, turn, ownWords);

        return new PostProgression(
            (guarded ? -subscriberDelta : subscriberDelta) * result.Multiplier,
            turn.Quality,
            viral,
            flop,
            skills,
            (views * result.Multiplier) + result.ViewsAdder,
            likes * result.Multiplier,
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

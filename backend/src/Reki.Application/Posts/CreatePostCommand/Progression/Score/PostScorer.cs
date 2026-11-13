using Reki.Application.Balance;
using Reki.Application.Posts.ML;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.CreatePostCommand.Progression.Score;

internal sealed class PostScorer(
    PostScoringContext context,
    PostTextAnalysis analysis,
    IEnumerable<IFlopTest> flopTests)
{
    public async Task<PostScore> ScoreAsync(GamePost post, World world, GameBalance balance, bool ownWords)
    {
        context.Current = new PostScoringData(world, balance);
        var skills = await analysis.SkillsAsync(post, world, ownWords);
        if (post.RepostId is not null)
        {
            return new PostScore(skills[0], skills[1], 1, false, null);
        }

        var emotion = await analysis.ClassifyAsync(ClassifierModels.EmotionModel, post.Text);
        var result = await flopTests.OrderBy(test => test.Priority)
            .ToAsyncEnumerable()
            .Select((IFlopTest test, CancellationToken _) => new ValueTask<FlopTestResult>(test.IsFlopAsync(post)))
            .FirstOrDefaultAsync(result => result.IsFlop);
        var reason = result?.Reason;

        string[] emotions = ["anger", "sadness", "surprise", "joy", "fear", "no_emotion"];
        var strongest = emotions.MaxBy(item => emotion[item]);

        var flopReason = reason ?? (skills[2] > 0 ? null : "off_topic");

        return new PostScore(skills[0], skills[1], skills[2], strongest is "joy" or "surprise", flopReason);
    }
}

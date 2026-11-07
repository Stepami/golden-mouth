using Reki.Application.Balance;
using Reki.Application.Posts.ML;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.CreatePostCommand.Score;

internal sealed class PostScorer(
    PostScoringContext context,
    PostTextAnalysis analysis,
    IEnumerable<IFlopTest> flopTests)
{
    public async Task<PostScore> ScoreAsync(GamePost post, World world, GameBalance balance, CancellationToken cancellationToken)
    {
        context.Current = new PostScoringData(world, balance, cancellationToken);
        var topic = $"{world.Interface.Title}. {world.Interface.Description}. {world.Player.Role}";
        var skills = await analysis.SkillsAsync(post.Text, topic, cancellationToken);
        var emotion = await analysis.ClassifyAsync(ClassifierModels.EmotionModel, post.Text);
        var result = await flopTests.OrderBy(test => test.Priority)
            .ToAsyncEnumerable()
            .Select((IFlopTest test, CancellationToken _) => new ValueTask<FlopTestResult>(test.IsFlopAsync(post)))
            .FirstOrDefaultAsync(result => result.IsFlop, cancellationToken);
        var reason = result?.Reason;

        var emoji = new Dictionary<string, string>
        {
            ["anger"] = "👎",
            ["sadness"] = "❤️",
            ["surprise"] = "🤔",
            ["joy"] = "🔥",
            ["fear"] = "🤔",
            ["no_emotion"] = "🔥",
        }.MaxBy(item => emotion[item.Key]).Value;

        return new PostScore(skills[0], skills[1], skills[2], emoji, reason);
    }
}

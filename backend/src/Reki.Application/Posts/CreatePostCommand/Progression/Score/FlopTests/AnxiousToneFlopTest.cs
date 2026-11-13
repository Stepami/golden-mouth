using Reki.Application.Posts.ML;
using Reki.Domain.Posts;

namespace Reki.Application.Posts.CreatePostCommand.Progression.Score.FlopTests;

internal sealed class AnxiousToneFlopTest(PostTextAnalysis analysis, PostScoringContext context) : IFlopTest
{
    public int Priority => context.Current.Balance.Difficulties[context.Current.World.Difficulty].ReasonPriority.Count + 1;

    public async Task<FlopTestResult> IsFlopAsync(GamePost post)
    {
        var emotion = await analysis.ClassifyAsync(ClassifierModels.EmotionModel, post.Text);
        return emotion["fear"] >= analysis.Threshold("anxious_tone")
            ? FlopTestResult.Flop("anxious_tone")
            : FlopTestResult.Ok;
    }
}

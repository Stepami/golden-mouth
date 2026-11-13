using Reki.Application.Posts.ML;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.CreatePostCommand.Progression.Score.FlopTests;

internal sealed class AnxiousToneFlopTest(
    PostTextAnalysis analysis,
    IClassifier classifier) : IFlopTest
{
    public int Priority => 4;

    public async ValueTask<FlopTestResult> IsFlopAsync(GamePost post, World world)
    {
        var emotion = await classifier.ClassifyAsync(ClassifierModels.EmotionModel, post.Text);
        return emotion["fear"] >= analysis.Threshold("anxious_tone", world)
            ? FlopTestResult.Flop("anxious_tone")
            : FlopTestResult.Ok;
    }
}

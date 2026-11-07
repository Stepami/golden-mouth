using Reki.Application.Posts.ML;
using Reki.Domain.Posts;

namespace Reki.Application.Posts.CreatePostCommand;

internal sealed class AnxiousToneFlopTest(PostTextAnalysis analysis) : IFlopTest
{
    public async Task<bool> IsFlopAsync(GamePost post)
    {
        var emotion = await analysis.ClassifyAsync(ClassifierModels.EmotionModel, post.Text);
        return emotion["fear"] >= analysis.Threshold("anxious_tone");
    }
}

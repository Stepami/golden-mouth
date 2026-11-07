using Reki.Application.Posts.ML;
using Reki.Domain.Posts;

namespace Reki.Application.Posts.CreatePostCommand;

internal sealed class AngryAbuseFlopTest(PostTextAnalysis analysis) : IFlopTest
{
    public async Task<bool> IsFlopAsync(GamePost post)
    {
        var text = PostTextAnalysis.WithoutQuotes(post.Text);
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var toxicity = await analysis.ClassifyAsync(ClassifierModels.ToxicityModel, text);
        var emotion = await analysis.ClassifyAsync(ClassifierModels.EmotionModel, text);
        return Math.Min(toxicity["insult"], emotion["anger"]) >= analysis.Threshold("angry_abuse");
    }
}

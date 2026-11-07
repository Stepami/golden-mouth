using Reki.Application.Posts.CreatePostCommand.Score;
using Reki.Application.Posts.ML;
using Reki.Domain.Posts;

namespace Reki.Application.Posts.CreatePostCommand.FlopTests;

internal sealed class AngryAbuseFlopTest(PostTextAnalysis analysis, PostScoringContext context) : IFlopTest
{
    public int Priority => context.Current.Balance.Difficulties[context.Current.World.Difficulty].ReasonPriority.ToList().IndexOf("angry_abuse");

    public async Task<FlopTestResult> IsFlopAsync(GamePost post)
    {
        var text = PostTextAnalysis.WithoutQuotes(post.Text);
        if (string.IsNullOrWhiteSpace(text))
        {
            return FlopTestResult.Ok;
        }

        var toxicity = await analysis.ClassifyAsync(ClassifierModels.ToxicityModel, text);
        var emotion = await analysis.ClassifyAsync(ClassifierModels.EmotionModel, text);
        return Math.Min(toxicity["insult"], emotion["anger"]) >= analysis.Threshold("angry_abuse")
            ? FlopTestResult.Flop("angry_abuse")
            : FlopTestResult.Ok;
    }
}

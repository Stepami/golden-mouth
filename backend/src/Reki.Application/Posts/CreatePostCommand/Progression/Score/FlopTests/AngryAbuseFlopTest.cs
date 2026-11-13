using System.Text.RegularExpressions;
using Reki.Application.Posts.ML;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.CreatePostCommand.Progression.Score.FlopTests;

internal sealed partial class AngryAbuseFlopTest(
    PostTextAnalysis analysis,
    IClassifier classifier) : IFlopTest
{
    public int Priority => 1;

    public async ValueTask<FlopTestResult> IsFlopAsync(GamePost post, World world)
    {
        var text = QuotedText().Replace(post.Text, string.Empty);
        if (string.IsNullOrWhiteSpace(text))
        {
            return FlopTestResult.Ok;
        }

        var toxicity = await classifier.ClassifyAsync(ClassifierModels.ToxicityModel, text);
        var emotion = await classifier.ClassifyAsync(ClassifierModels.EmotionModel, text);
        return Math.Min(toxicity["insult"], emotion["anger"]) >= analysis.Threshold("angry_abuse", world)
            ? FlopTestResult.Flop("angry_abuse")
            : FlopTestResult.Ok;
    }

    [GeneratedRegex("«[^»]*»|„[^“]*“|\"[^\"]*\"|“[^”]*”", RegexOptions.Singleline)]
    private static partial Regex QuotedText();
}

using Reki.Application.Posts.ML;
using Reki.Domain.Posts;

namespace Reki.Application.Posts.CreatePostCommand.Progression.Score.FlopTests;

internal sealed class SadToneFlopTest(PostTextAnalysis analysis, PostScoringContext context) : IFlopTest
{
    public int Priority => context.Current.Balance.Difficulties[context.Current.World.Difficulty].ReasonPriority.Count;

    public async Task<FlopTestResult> IsFlopAsync(GamePost post)
    {
        var emotion = await analysis.ClassifyAsync(ClassifierModels.EmotionModel, post.Text);
        var sad = emotion["sadness"];
        var fear = emotion["fear"];
        var world = context.Current.World;
        var tieBreak = context.Current.Balance.Difficulties[world.Difficulty].EmotionTieBreak;
        var flop = sad >= analysis.Threshold("sad_tone") &&
               (fear < analysis.Threshold("anxious_tone") || sad > fear || (sad.CompareTo(fear) == 0 && tieBreak == "sad_tone"));
        return flop ? FlopTestResult.Flop("sad_tone") : FlopTestResult.Ok;
    }
}

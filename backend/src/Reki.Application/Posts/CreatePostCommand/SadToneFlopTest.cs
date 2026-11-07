using Reki.Application.Posts.ML;
using Reki.Domain.Posts;

namespace Reki.Application.Posts.CreatePostCommand;

internal sealed class SadToneFlopTest(PostTextAnalysis analysis, PostScoringContext context) : IFlopTest
{
    public async Task<bool> IsFlopAsync(GamePost post)
    {
        var emotion = await analysis.ClassifyAsync(ClassifierModels.EmotionModel, post.Text);
        var sad = emotion["sadness"];
        var fear = emotion["fear"];
        var world = context.Current.World;
        var tieBreak = context.Current.Balance.Difficulties[world.Difficulty].EmotionTieBreak;
        return sad >= analysis.Threshold("sad_tone") &&
               (fear < analysis.Threshold("anxious_tone") || sad > fear || (sad.CompareTo(fear) == 0 && tieBreak == "sad_tone"));
    }
}

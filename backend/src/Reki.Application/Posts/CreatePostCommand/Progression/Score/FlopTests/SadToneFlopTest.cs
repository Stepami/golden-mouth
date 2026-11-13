using Microsoft.Extensions.Options;
using Reki.Application.Balance;
using Reki.Application.Posts.ML;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.CreatePostCommand.Progression.Score.FlopTests;

internal sealed class SadToneFlopTest(
    PostTextAnalysis analysis,
    IClassifier classifier,
    IOptionsMonitor<GameBalance> balance) : IFlopTest
{
    public int Priority => 3;

    public async ValueTask<FlopTestResult> IsFlopAsync(GamePost post, World world)
    {
        var emotion = await classifier.ClassifyAsync(ClassifierModels.EmotionModel, post.Text);
        var sad = emotion["sadness"];
        var fear = emotion["fear"];
        var tieBreak = balance.CurrentValue.Difficulties[world.Difficulty].EmotionTieBreak;
        var flop = sad >= analysis.Threshold("sad_tone", world) &&
               (fear < analysis.Threshold("anxious_tone", world) || sad > fear || (sad.CompareTo(fear) == 0 && tieBreak == "sad_tone"));
        return flop ? FlopTestResult.Flop("sad_tone") : FlopTestResult.Ok;
    }
}

using Reki.Application.Balance;
using Reki.Application.Posts.ML;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.CreatePostCommand;

internal sealed class PostScorer(
    PostScoringContext context,
    PostTextAnalysis analysis,
    IEnumerable<IFlopTest> flopTests)
{
    public async Task<PostScore> ScoreAsync(GamePost post, World world, ReadOnlyMemory<float> embedding, GameBalance balance, CancellationToken cancellationToken)
    {
        context.Current = new PostScoringData(world, balance, embedding, cancellationToken);
        var topic = $"{world.Interface.Title}. {world.Interface.Description}. {world.Player.Role}";
        var skills = await analysis.SkillsAsync(post.Text, topic, cancellationToken);
        var emotion = await analysis.ClassifyAsync(ClassifierModels.EmotionModel, post.Text);
        var reason = (string?)null;
        var priority = balance.Difficulties[world.Difficulty].ReasonPriority.ToList();
        foreach (var test in flopTests.OrderBy(test => test switch
                 {
                     AngryAbuseFlopTest => priority.IndexOf("angry_abuse"),
                     NearCopyFlopTest => priority.IndexOf("near_copy"),
                     SadToneFlopTest => priority.Count,
                     _ => priority.Count + 1,
                 }))
        {
            if (await test.IsFlopAsync(post))
            {
                reason = test switch
                {
                    AngryAbuseFlopTest => "angry_abuse",
                    NearCopyFlopTest => "near_copy",
                    SadToneFlopTest => "sad_tone",
                    AnxiousToneFlopTest => "anxious_tone",
                    _ => null,
                };
                if (reason is not null)
                {
                    break;
                }
            }
        }

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

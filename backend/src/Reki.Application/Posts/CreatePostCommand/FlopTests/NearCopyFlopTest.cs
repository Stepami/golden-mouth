using Reki.Application.Posts.CreatePostCommand.Score;
using Reki.Domain.Posts;

namespace Reki.Application.Posts.CreatePostCommand.FlopTests;

internal sealed class NearCopyFlopTest(
    IPostRepository posts,
    PostTextAnalysis analysis,
    PostScoringContext context) : IFlopTest
{
    public int Priority => context.Current.Balance.Difficulties[context.Current.World.Difficulty].ReasonPriority.ToList().IndexOf("near_copy");

    public async Task<FlopTestResult> IsFlopAsync(GamePost post)
    {
        var current = context.Current;
        var history = await posts.FindSimilarPostsAsync(post, analysis.Threshold("near_copy_cosine"), current.CancellationToken);
        return history.Count > 0
            ? FlopTestResult.Flop("near_copy")
            : FlopTestResult.Ok;
    }
}

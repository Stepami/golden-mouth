using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.CreatePostCommand.Progression.Score.FlopTests;

internal sealed class NearCopyFlopTest(
    IPostRepository posts,
    PostTextAnalysis analysis) : IFlopTest
{
    public int Priority => 2;

    public async ValueTask<FlopTestResult> IsFlopAsync(GamePost post, World world)
    {
        var history = await posts.FindSimilarPostsAsync(
            post,
            analysis.Threshold("near_copy_cosine", world));
        return history.Count > 0
            ? FlopTestResult.Flop("near_copy")
            : FlopTestResult.Ok;
    }
}

using Reki.Domain.Posts;

namespace Reki.Application.Posts.CreatePostCommand.Progression.Score.FlopTests;

internal sealed class NearCopyFlopTest(
    IPostRepository posts,
    PostTextAnalysis analysis,
    PostScoringContext context) : IFlopTest
{
    public int Priority => context.Current.Balance.Difficulties[context.Current.World.Difficulty].ReasonPriority.ToList().IndexOf("near_copy");

    public async Task<FlopTestResult> IsFlopAsync(GamePost post)
    {
        var history = await posts.FindSimilarPostsAsync(
            post,
            analysis.Threshold("near_copy_cosine"));
        return history.Count > 0
            ? FlopTestResult.Flop("near_copy")
            : FlopTestResult.Ok;
    }
}

using System.Text.RegularExpressions;
using Reki.Application.Posts.CreatePostCommand.Score;
using Reki.Domain.Posts;

namespace Reki.Application.Posts.CreatePostCommand.FlopTests;

internal sealed partial class NearCopyFlopTest(
    IPostRepository posts,
    PostTextAnalysis analysis,
    PostScoringContext context) : IFlopTest
{
    public int Priority => context.Current.Balance.Difficulties[context.Current.World.Difficulty].ReasonPriority.ToList().IndexOf("near_copy");

    public async Task<FlopTestResult> IsFlopAsync(GamePost post)
    {
        var current = context.Current;
        var history = await posts.FindSimilarPostsAsync(post, analysis.Threshold("near_copy_cosine"), current.CancellationToken);
        var numbers = Numbers(post.Text);
        return history.Any(candidate => numbers.SequenceEqual(Numbers(candidate.Text)))
            ? FlopTestResult.Flop("near_copy")
            : FlopTestResult.Ok;
    }

    private static string[] Numbers(string text) => Number().Matches(text).Select(match => match.Value).ToArray();

    [GeneratedRegex(@"\d+(?:[.,]\d+)*", RegexOptions.CultureInvariant)]
    private static partial Regex Number();
}

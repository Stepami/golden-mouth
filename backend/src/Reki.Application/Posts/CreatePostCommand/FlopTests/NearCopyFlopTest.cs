using System.Text.RegularExpressions;
using Reki.Application.Posts.CreatePostCommand.Score;
using Reki.Application.Posts.ML;
using Reki.Domain.Posts;

namespace Reki.Application.Posts.CreatePostCommand.FlopTests;

internal sealed partial class NearCopyFlopTest(
    IPostRepository posts,
    INliScorer nli,
    PostTextAnalysis analysis,
    PostScoringContext context) : IFlopTest
{
    public int Priority => context.Current.Balance.Difficulties[context.Current.World.Difficulty].ReasonPriority.ToList().IndexOf("near_copy");

    public async Task<FlopTestResult> IsFlopAsync(GamePost post)
    {
        var current = context.Current;
        var history = await posts.FindSimilarPostsAsync(post, analysis.Threshold("near_copy_cosine"), current.CancellationToken);
        var numbers = Numbers(post.Text);
        foreach (var candidateText in history.Select(candidate => candidate.Text))
        {
            if (!numbers.SequenceEqual(Numbers(candidateText)))
            {
                continue;
            }

            if (await EntailsAsync(post.Text, candidateText) && await EntailsAsync(candidateText, post.Text))
            {
                return FlopTestResult.Flop("near_copy");
            }
        }

        return FlopTestResult.Ok;
    }

    private static string[] Numbers(string text) => Number().Matches(text).Select(match => match.Value).ToArray();

    [GeneratedRegex(@"\d+(?:[.,]\d+)*", RegexOptions.CultureInvariant)]
    private static partial Regex Number();

    private async Task<bool> EntailsAsync(string premise, string hypothesis)
    {
        var hypotheses = PostTextAnalysis.Windows(hypothesis).ToArray();
        foreach (var part in PostTextAnalysis.Windows(premise))
        {
            var scores = await nli.ScoreAsync(part, hypotheses, context.Current.CancellationToken);
            if (scores.Max(score => score.Entailment) < analysis.Threshold("near_copy_nli"))
            {
                return false;
            }
        }

        return true;
    }
}

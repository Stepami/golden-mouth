using System.Numerics.Tensors;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Reki.Application.Posts.AI;
using Reki.Application.Posts.ML;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.CreatePostCommand.Score;

internal sealed partial class PostTextAnalysis(
    IClassifier classifier,
    INliScorer nli,
    IRekiEmbeddingGenerator<World> worldEmbedder,
    IRekiEmbeddingGenerator<GamePost> postEmbedder,
    PostScoringContext context)
{
    public static string WithoutQuotes(string text) => QuotedText().Replace(text, string.Empty);

    public double Threshold(string key)
    {
        var world = context.Current.World;
        var profile = context.Current.Balance.Difficulties[world.Difficulty];
        return profile.Thresholds[key] - (profile.Drama[world.Player.Drama].ThresholdSteps * profile.ThresholdReductionPerStep[key]);
    }

    public Task<IReadOnlyDictionary<string, double>> ClassifyAsync(string model, string text) =>
        classifier.ClassifyAsync(model, text, context.Current.CancellationToken);

    public async Task<double[]> SkillsAsync(
        GamePost post,
        World world,
        CancellationToken cancellationToken)
    {
        string[] hypotheses = ["Автор смело выражает своё мнение.", "Текст содержит юмор или шутку."];
        var scores = await nli.ScoreAsync(post.Text, hypotheses, cancellationToken);

        var postVector = await postEmbedder.GenerateVectorAsync(post, cancellationToken: cancellationToken);
        var worldVector = await worldEmbedder.GenerateVectorAsync(world, cancellationToken: cancellationToken);
        var similarity = TensorPrimitives.CosineSimilarity(postVector.Span, worldVector.Span);
        var topic = similarity >= context.Current.Balance.Difficulties[world.Difficulty].TopicMinCosine ? 1d : 0d;
        return [scores[0].Entailment, scores[1].Entailment, topic];
    }

    [GeneratedRegex("«[^»]*»|„[^“]*“|\"[^\"]*\"|“[^”]*”", RegexOptions.Singleline)]
    private static partial Regex QuotedText();
}

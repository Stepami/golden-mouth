using System.Numerics.Tensors;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Reki.Application.Balance;
using Reki.Application.Posts.AI;
using Reki.Application.Posts.ML;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.CreatePostCommand.Progression.Score;

internal sealed class PostTextAnalysis(
    INliScorer nli,
    IRekiEmbeddingGenerator<World> worldEmbedder,
    IRekiEmbeddingGenerator<GamePost> postEmbedder,
    IOptionsMonitor<GameBalance> balance)
{
    public double Threshold(string key, World world)
    {
        var profile = balance.CurrentValue.Difficulties[world.Difficulty];
        return profile.Thresholds[key] - (profile.Drama[world.Player.Drama].ThresholdSteps * profile.ThresholdReductionPerStep[key]);
    }

    public async Task<double[]> SkillsAsync(
        GamePost post,
        World world,
        bool ownWords)
    {
        if (post.RepostId is not null && string.IsNullOrWhiteSpace(post.Text))
        {
            return [0, 0, 1];
        }

        string[] hypotheses = ["Автор смело выражает своё мнение.", "Текст содержит юмор или шутку."];
        var scores = await nli.ScoreAsync(post.Text, hypotheses);

        if (!ownWords || post.RepostId is not null)
        {
            return [scores[0].Entailment, scores[1].Entailment, 1d];
        }

        var postVector = await postEmbedder.GenerateVectorAsync(post);
        var worldVector = await worldEmbedder.GenerateVectorAsync(world);
        var similarity = TensorPrimitives.CosineSimilarity(postVector.Span, worldVector.Span);
        var topic = similarity >= (float)balance.CurrentValue.Difficulties[world.Difficulty].TopicMinCosine ? 1d : 0d;

        return [scores[0].Entailment, scores[1].Entailment, topic];
    }
}

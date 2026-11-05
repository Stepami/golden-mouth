namespace Reki.Application.Posts.ML;

public interface INliScorer
{
    ValueTask<IReadOnlyList<NliScores>> ScoreAsync(
        string premise,
        IReadOnlyList<string> hypotheses,
        CancellationToken cancellationToken);
}

public sealed record NliScores(double Entailment, double NotEntailment);
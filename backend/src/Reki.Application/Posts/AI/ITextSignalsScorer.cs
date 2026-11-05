namespace Reki.Application.Posts.AI;

public interface ITextSignalsScorer
{
    ValueTask<TextSignals> ScoreAsync(string text, CancellationToken cancellationToken);
}

public sealed record TextSignals(
    IReadOnlyDictionary<string, double> Toxicity,
    IReadOnlyDictionary<string, double> Emotion);

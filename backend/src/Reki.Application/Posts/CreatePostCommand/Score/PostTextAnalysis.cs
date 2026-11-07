using System.Text.RegularExpressions;
using Reki.Application.Posts.ML;

namespace Reki.Application.Posts.CreatePostCommand.Score;

internal sealed partial class PostTextAnalysis(
    IClassifier classifier,
    INliScorer nli,
    PostScoringContext context)
{
    public static IEnumerable<string> Windows(string text)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (var start = 0; start < words.Length; start += 80)
        {
            yield return string.Join(' ', words.Skip(start).Take(100));
        }
    }

    public static string WithoutQuotes(string text) => QuotedText().Replace(text, string.Empty);

    public double Threshold(string key)
    {
        var world = context.Current.World;
        var profile = context.Current.Balance.Difficulties[world.Difficulty];
        return profile.Thresholds[key] - (profile.Drama[world.Player.Drama].ThresholdSteps * profile.ThresholdReductionPerStep[key]);
    }

    public async Task<IReadOnlyDictionary<string, double>> ClassifyAsync(string model, string text)
    {
        var scores = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var window in Windows(text))
        {
            var values = await classifier.ClassifyAsync(model, window, context.Current.CancellationToken);
            Merge(scores, values);
        }

        return scores;
    }

    public async Task<double[]> SkillsAsync(string text, string topic, CancellationToken cancellationToken)
    {
        string[] hypotheses = ["Автор смело выражает своё мнение.", "Текст содержит юмор или шутку.", $"Текст связан с темой: {topic}"];
        var scores = new double[hypotheses.Length];
        foreach (var window in Windows(text))
        {
            var result = await nli.ScoreAsync(window, hypotheses, cancellationToken);
            MergeSkills(scores, result);
        }

        return scores;
    }

    private static void Merge(Dictionary<string, double> scores, IReadOnlyDictionary<string, double> values)
    {
        foreach (var (key, value) in values)
        {
            scores[key] = Math.Max(scores.GetValueOrDefault(key), value);
        }
    }

    private static void MergeSkills(double[] scores, IReadOnlyList<NliScores> result)
    {
        for (var index = 0; index < scores.Length; index++)
        {
            scores[index] = Math.Max(scores[index], result[index].Entailment);
        }
    }

    [GeneratedRegex("«[^»]*»|„[^“]*“|\"[^\"]*\"|“[^”]*”", RegexOptions.Singleline)]
    private static partial Regex QuotedText();
}

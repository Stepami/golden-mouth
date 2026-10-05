namespace Reki.Domain.Social;

public sealed record ChallengePerformance(
    int PostCount,
    double QualityTotal,
    double ReachRatioTotal)
{
    public static ChallengePerformance Empty { get; } = new(0, 0, 0);

    public double Score => PostCount == 0
        ? 0
        : 10 * ((0.7 * QualityTotal / PostCount) + (0.3 * ReachRatioTotal / PostCount));

    public ChallengePerformance Add(double quality, double reachRatio)
    {
        return new ChallengePerformance(
            PostCount + 1,
            QualityTotal + Math.Clamp(quality, 0, 1),
            ReachRatioTotal + Math.Max(0, reachRatio));
    }

    public static double CalculatePostScore(double quality, double reachRatio)
    {
        return 10 * ((0.7 * Math.Clamp(quality, 0, 1)) + (0.3 * Math.Max(0, reachRatio)));
    }
}

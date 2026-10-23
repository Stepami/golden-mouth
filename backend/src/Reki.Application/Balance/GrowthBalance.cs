namespace Reki.Application.Balance;

public sealed record GrowthBalance(
    double Power,
    double Coefficient,
    IReadOnlyList<PostDecayBalance> Decay,
    long StarterBelowSubscribers,
    double StarterMultiplier,
    double OwnWordsMultiplier,
    int RestedAfterSkippedDays,
    int RestedFirstPosts,
    double RestedMultiplier,
    long StartingSubscribers);

namespace Reki.Application.Balance;

public sealed record GrowthBalance(
    double Power,
    double Coefficient,
    long StarterBelowSubscribers,
    double StarterMultiplier,
    double OwnWordsMultiplier,
    long StartingSubscribers);

namespace Reki.Domain.Posts;

public sealed record PostBoosterEffect(
    long SubscriberDeltaWithoutBooster,
    long ViewsWithoutBooster,
    int? Multiplier = null,
    int? ExtraViews = null);

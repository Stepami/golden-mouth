namespace Reki.Application.Posts.Responses.PostResult;

public sealed record BoosterEffectResponse(
    int? Multiplier,
    int? ExtraViews,
    long SubscriberDeltaWithoutBooster,
    long ViewsWithoutBooster);

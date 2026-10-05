namespace Reki.Domain.Posts;

public sealed record GamePost(
    string Id,
    string PlayerId,
    string Text,
    long Views,
    IReadOnlyList<GameComment> Comments,
    DateTimeOffset CreatedAtUtc,
    long SubscriberDelta,
    bool Viral,
    bool Flop);


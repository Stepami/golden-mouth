using Reki.Domain.Player;

namespace Reki.Domain.Posts;

public sealed record GamePost(
    PostId Id,
    PlayerId PlayerId,
    string Text,
    long Views,
    IReadOnlyList<GameComment> Comments,
    DateTimeOffset CreatedAtUtc,
    long SubscriberDelta,
    double Quality,
    double ReachRatio,
    bool Viral,
    bool Flop);


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
    bool Viral,
    bool Flop);


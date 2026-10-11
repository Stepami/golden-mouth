using Reki.Domain.Player;
using Reki.Domain.Worlds;

namespace Reki.Domain.Posts;

public sealed record GamePost(
    PostId Id,
    PlayerId PlayerId,
    WorldId WorldId,
    string Text,
    long Views,
    IReadOnlyList<GameComment> Comments,
    IReadOnlyList<ThreadReply> ThreadReplies,
    DateTimeOffset CreatedAtUtc,
    long SubscriberDelta,
    double Quality,
    double ReachRatio,
    bool Viral,
    bool Flop)
{
    public ThreadReply? FindReply(string replyId) =>
        ThreadReplies.FirstOrDefault(item => string.Equals(item.ReplyId, replyId, StringComparison.Ordinal));
}
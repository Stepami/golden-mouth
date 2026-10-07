using Reki.Domain.Player;

namespace Reki.Domain.Posts;

public sealed record GamePost(
    PostId Id,
    PlayerId PlayerId,
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
    public IReadOnlyList<ThreadReply> GetThreadReplies()
    {
        // Mongo can hydrate null for fields absent from legacy documents,
        // despite the non-nullable domain contract.
        // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
        return ThreadReplies ?? [];
    }

    public ThreadReply? FindReply(string replyId) =>
        GetThreadReplies().FirstOrDefault(item => string.Equals(item.ReplyId, replyId, StringComparison.Ordinal));
}


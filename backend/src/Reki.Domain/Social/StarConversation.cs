namespace Reki.Domain.Social;

public sealed record StarConversation(
    StarConversationId Id,
    string PlayerId,
    string StarId,
    IReadOnlyList<string> Memory,
    IReadOnlyList<DirectMessage> Messages,
    int RepliesUsed,
    DateTimeOffset PeriodStartedAtUtc);


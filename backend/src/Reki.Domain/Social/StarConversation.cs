using Reki.Domain.Player;

namespace Reki.Domain.Social;

public sealed record StarConversation(
    StarConversationId Id,
    PlayerId PlayerId,
    StarId StarId,
    IReadOnlyList<string> Memory,
    IReadOnlyList<DirectMessage> Messages,
    int RepliesUsed,
    DateTimeOffset PeriodStartedAtUtc);


using Reki.Domain.Player;
using Reki.Domain.Worlds;

namespace Reki.Domain.Social;

public sealed record StarConversation(
    StarConversationId Id,
    PlayerId PlayerId,
    WorldId WorldId,
    StarId StarId,
    IReadOnlyList<string> Memory,
    IReadOnlyList<DirectMessage> Messages,
    int RepliesUsed,
    DateTimeOffset PeriodStartedAtUtc);
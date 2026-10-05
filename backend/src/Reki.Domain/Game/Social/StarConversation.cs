namespace Reki.Domain.Game.Social;

public sealed record StarConversation(
    string PlayerId,
    string StarId,
    IReadOnlyList<string> Memory,
    IReadOnlyList<DirectMessage> Messages,
    int RepliesUsed,
    DateTimeOffset PeriodStartedAtUtc);


namespace Reki.Application.Player;

public sealed record InviteState(
    int InviterBonuses,
    int LegacyInviterBonuses,
    int InviteePosts,
    int PendingFirstPostBonus,
    bool HasAttribution);
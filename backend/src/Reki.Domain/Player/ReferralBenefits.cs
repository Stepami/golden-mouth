namespace Reki.Domain.Player;

// A read projection of invitations, never persisted in the player document.
public sealed record ReferralBenefits(int DailyPosts, int FirstPostBonus, bool HasAttribution)
{
    public static ReferralBenefits Empty { get; } = new(0, 0, false);
}
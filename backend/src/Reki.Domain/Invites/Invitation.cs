using Reki.Domain.Player;

namespace Reki.Domain.Invites;

public sealed record Invitation(
    InvitationId Id,
    PlayerId InviterId,
    PlayerId InviteeId,
    DateTimeOffset CreatedAtUtc,
    bool InviterBonusUsed,
    bool InviteeBonusUsed,
    int BonusAmount,
    DateTimeOffset? InviterBonusUsedAtUtc = null,
    DateTimeOffset? InviteeBonusUsedAtUtc = null)
{
    public static Invitation Create(
        PlayerId inviterId,
        PlayerId inviteeId,
        DateTimeOffset createdAtUtc,
        int bonusAmount)
    {
        if (inviterId == inviteeId)
        {
            throw new ArgumentException("A player cannot invite their own account.", nameof(inviteeId));
        }

        return new Invitation(
            InvitationId.New(),
            inviterId,
            inviteeId,
            createdAtUtc.ToUniversalTime(),
            false,
            false,
            bonusAmount);
    }

    public Invitation UseInviterBonus(DateTimeOffset now) =>
        InviterBonusUsed ? this : this with { InviterBonusUsed = true, InviterBonusUsedAtUtc = now.ToUniversalTime() };

    public Invitation UseInviteeBonus(DateTimeOffset now) =>
        InviteeBonusUsed ? this : this with { InviteeBonusUsed = true, InviteeBonusUsedAtUtc = now.ToUniversalTime() };
}
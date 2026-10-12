using Reki.Domain.Player;

namespace Reki.Domain.Invites;

public sealed record Invitation(
    InvitationId Id,
    PlayerId InviterId,
    PlayerId InviteeId,
    DateTimeOffset CreatedAtUtc,
    bool InviterBonusUsed,
    bool InviteeBonusUsed)
{
    public static Invitation Create(PlayerId inviterId, PlayerId inviteeId, DateTimeOffset createdAtUtc)
    {
        if (inviterId == inviteeId)
        {
            throw new ArgumentException("A player cannot invite their own account.", nameof(inviteeId));
        }

        return new Invitation(InvitationId.New(), inviterId, inviteeId, createdAtUtc.ToUniversalTime(), false, false);
    }

    public Invitation UseInviterBonus() => InviterBonusUsed ? this : this with { InviterBonusUsed = true };

    public Invitation UseInviteeBonus() => InviteeBonusUsed ? this : this with { InviteeBonusUsed = true };
}
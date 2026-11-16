namespace Reki.Domain.Invites;

using Reki.Domain.Player;

public sealed class Invitation(
    InvitationId id,
    PlayerId inviterId,
    PlayerId inviteeId,
    DateTimeOffset createdAtUtc,
    bool inviterBonusUsed,
    bool inviteeBonusUsed,
    int bonusAmount,
    DateTimeOffset? inviterBonusUsedAtUtc = null,
    DateTimeOffset? inviteeBonusUsedAtUtc = null,
    int version = 0)
{
    public InvitationId Id { get; private set; } = id;
    public PlayerId InviterId { get; private set; } = inviterId;
    public PlayerId InviteeId { get; private set; } = inviteeId;
    public DateTimeOffset CreatedAtUtc { get; private set; } = createdAtUtc;
    public bool InviterBonusUsed { get; private set; } = inviterBonusUsed;
    public bool InviteeBonusUsed { get; private set; } = inviteeBonusUsed;
    public int BonusAmount { get; private set; } = bonusAmount;
    public DateTimeOffset? InviterBonusUsedAtUtc { get; private set; } = inviterBonusUsedAtUtc;
    public DateTimeOffset? InviteeBonusUsedAtUtc { get; private set; } = inviteeBonusUsedAtUtc;
    public int Version { get; private set; } = version;

    public static Invitation Create(PlayerId inviterId, PlayerId inviteeId, DateTimeOffset createdAtUtc, int bonusAmount)
    {
        if (inviterId == inviteeId)
        {
            throw new ArgumentException("A player cannot invite their own account.", nameof(inviteeId));
        }

        return new Invitation(InvitationId.New(), inviterId, inviteeId, createdAtUtc.ToUniversalTime(), false, false, bonusAmount);
    }

    public Invitation UseInviterBonus(DateTimeOffset now)
    {
        if (!InviterBonusUsed)
        {
            InviterBonusUsed = true;
            InviterBonusUsedAtUtc = now.ToUniversalTime();
            Version++;
        }

        return this;
    }

    public Invitation UseInviteeBonus(DateTimeOffset now)
    {
        if (!InviteeBonusUsed)
        {
            InviteeBonusUsed = true;
            InviteeBonusUsedAtUtc = now.ToUniversalTime();
            Version++;
        }

        return this;
    }
}

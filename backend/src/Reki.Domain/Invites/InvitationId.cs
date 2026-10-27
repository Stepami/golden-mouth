using Vogen;

namespace Reki.Domain.Invites;

[ValueObject<Guid>(comparison: ComparisonGeneration.Omit)]
public readonly partial struct InvitationId
{
    public static InvitationId New() => From(Guid.NewGuid());

    public static InvitationId From(string value) => From(Guid.Parse(value));

    public override string ToString() => ToString("N", System.Globalization.CultureInfo.InvariantCulture);
}
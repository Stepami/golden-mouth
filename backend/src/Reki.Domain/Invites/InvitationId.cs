using Reki.Domain.Common;
using Vogen;

namespace Reki.Domain.Invites;

[ValueObject<string>(comparison: ComparisonGeneration.Omit)]
public readonly partial struct InvitationId
{
    public static InvitationId New() => From(Guid.NewGuid().ToString("N"));

    private static Validation Validate(string value) => IdentifierValidation.String(value);
}
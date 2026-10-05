using Vogen;

namespace Reki.Domain.Common;

internal static class IdentifierValidation
{
    public static Validation String(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? Validation.Invalid("Identifier must not be empty.")
            : Validation.Ok;
    }

    public static Validation Positive(long value)
    {
        return value > 0
            ? Validation.Ok
            : Validation.Invalid("Identifier must be positive.");
    }
}

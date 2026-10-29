using Vogen;

namespace Reki.Domain.Player;

[ValueObject<AddressType>(comparison: ComparisonGeneration.Omit)]
public readonly partial struct Address
{
    // Vogen 8 emits integer literals for enum Instance attributes; use its validated factory instead.
    public static Address She { get; } = From(AddressType.she);

    public static Address He { get; } = From(AddressType.he);

    public static Address Nick { get; } = From(AddressType.nick);

    public string GetPlayerNamePolicy(string playerName) => Value switch
    {
        AddressType.he => $"К персонажу игрока {playerName} обращайся в мужском роде.",
        AddressType.she => $"К персонажу игрока {playerName} обращайся в женском роде.",
        _ => $"К персонажу игрока {playerName} обращайся по имени, избегая форм с указанием рода.",
    };

    // Vogen 8 does not generate this IConvertible member for enum-backed values.
    public string ToString(IFormatProvider? provider) => ToString();

    private static Validation Validate(AddressType value) =>
        Enum.IsDefined(value)
            ? Validation.Ok
            : Validation.Invalid("Address must be she, he or nick.");
}

using Vogen;

namespace Reki.Domain.Player;

[ValueObject<string>(comparison: ComparisonGeneration.Omit)]
[Instance("She", "she")]
[Instance("He", "he")]
[Instance("Nick", "nick")]
public readonly partial struct Address
{
    public string GetPlayerNamePolicy(string playerName) => Value switch
    {
        "he" => $"К персонажу игрока {playerName} обращайся в мужском роде.",
        "she" => $"К персонажу игрока {playerName} обращайся в женском роде.",
        _ => $"К персонажу игрока {playerName} обращайся по имени, избегая форм с указанием рода.",
    };

    private static Validation Validate(string value) =>
        value is "she" or "he" or "nick"
            ? Validation.Ok
            : Validation.Invalid("Address must be she, he or nick.");
}

using Vogen;

namespace Reki.Domain.Posts;

/// <summary>Идентификатор автора в формате Player_ID или Star_ID.</summary>
[ValueObject<string>(comparison: ComparisonGeneration.Omit)]
public readonly partial struct AuthorId
{
    private const string PlayerPrefix = "Player_";
    private const string StarPrefix = "Star_";

    /// <summary>Gets the kind of the author.</summary>
    public AuthorKind Kind => Value.StartsWith(PlayerPrefix, StringComparison.Ordinal)
        ? AuthorKind.Player
        : AuthorKind.Star;

    /// <summary>Gets the original identifier without the author kind prefix.</summary>
    public string Id => Value[(Value.IndexOf('_') + 1)..];

    /// <summary>Создаёт идентификатор автора из типа и исходного идентификатора.</summary>
    /// <param name="kind">Тип автора.</param>
    /// <param name="id">Исходный идентификатор автора.</param>
    /// <returns>Проверенный идентификатор автора.</returns>
    public static AuthorId Create(AuthorKind kind, string id) => From(kind switch
    {
        AuthorKind.Player => $"{PlayerPrefix}{id}",
        AuthorKind.Star => $"{StarPrefix}{id}",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    });

    private static Validation Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            (!value.StartsWith(PlayerPrefix, StringComparison.Ordinal) &&
             !value.StartsWith(StarPrefix, StringComparison.Ordinal)) ||
            string.IsNullOrWhiteSpace(value[(value.IndexOf('_') + 1)..]))
        {
            return Validation.Invalid("Author kind and identifier are required.");
        }

        return Validation.Ok;
    }
}

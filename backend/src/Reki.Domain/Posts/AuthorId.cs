using Reki.Domain.Player;
using Reki.Domain.Social;
using Vogen;

namespace Reki.Domain.Posts;

/// <summary>Идентификатор автора в формате Player_ID или Star_ID.</summary>
[ValueObject<string>(comparison: ComparisonGeneration.Omit)]
public readonly partial struct AuthorId
{
    private const string PlayerPrefix = "Player_";
    private const string StarPrefix = "Star_";

    /// <summary>Создаёт идентификатор автора из <see cref="PlayerId"/>.</summary>
    /// <param name="playerId">Исходный идентификатор игрока.</param>
    /// <returns>Проверенный идентификатор автора-игрока.</returns>
    public static AuthorId Create(PlayerId playerId) =>
        From($"{AuthorKind.Player}_{playerId.Value}");

    /// <summary>Создаёт идентификатор автора из <see cref="StarId"/>.</summary>
    /// <param name="starId">Исходный идентификатор AI звезды.</param>
    /// <returns>Проверенный идентификатор автора-звезды.</returns>
    public static AuthorId Create(StarId starId) =>
        From($"{AuthorKind.Star}_{starId.Value}");

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

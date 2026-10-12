using Reki.Domain.Player;
using Reki.Domain.Social;
using Vogen;

namespace Reki.Domain.Posts;

/// <summary>Идентификатор автора в формате Player_ID или Star_ID.</summary>
[ValueObject<string>(comparison: ComparisonGeneration.Omit)]
public readonly partial struct AuthorId
{
    /// <summary>Создаёт идентификатор автора из <see cref="PlayerId"/>.</summary>
    /// <param name="playerId">Исходный идентификатор игрока.</param>
    /// <returns>Проверенный идентификатор автора-игрока.</returns>
    internal static AuthorId From(PlayerId playerId) =>
        From($"{AuthorKind.Player}_{playerId.Value}");

    /// <summary>Создаёт идентификатор автора из <see cref="StarId"/>.</summary>
    /// <param name="starId">Исходный идентификатор AI звезды.</param>
    /// <returns>Проверенный идентификатор автора-звезды.</returns>
    internal static AuthorId From(StarId starId) =>
        From($"{AuthorKind.Star}_{starId.Value}");

    private static Validation Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            (!value.StartsWith(nameof(AuthorKind.Player), StringComparison.Ordinal) &&
             !value.StartsWith(nameof(AuthorKind.Star), StringComparison.Ordinal)) ||
            string.IsNullOrWhiteSpace(value[(value.IndexOf('_') + 1)..]))
        {
            return Validation.Invalid("Author kind and identifier are required.");
        }

        return Validation.Ok;
    }
}

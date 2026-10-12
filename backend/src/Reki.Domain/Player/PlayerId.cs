using Reki.Domain.Common;
using Reki.Domain.Posts;
using Vogen;

namespace Reki.Domain.Player;

[ValueObject<string>(comparison: ComparisonGeneration.Omit)]
public readonly partial struct PlayerId
{
    public static PlayerId New() => From(Guid.NewGuid().ToString("N"));

    /// <summary>Преобразует идентификатор игрока в идентификатор автора.</summary>
    /// <returns>Идентификатор автора с типом игрока.</returns>
    public AuthorId ToAuthorId() => AuthorId.From(this);

    private static Validation Validate(string value) => IdentifierValidation.String(value);
}

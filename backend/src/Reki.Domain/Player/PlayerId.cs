using Reki.Domain.Posts;
using Vogen;

namespace Reki.Domain.Player;

[ValueObject<Guid>(comparison: ComparisonGeneration.Omit)]
public readonly partial struct PlayerId
{
    public static PlayerId New() => From(Guid.NewGuid());

    public static PlayerId From(string value) => From(Guid.Parse(value));

    /// <summary>Преобразует идентификатор игрока в идентификатор автора.</summary>
    /// <returns>Идентификатор автора с типом игрока.</returns>
    public AuthorId ToAuthorId() => AuthorId.From(this);

    public override string ToString() => ToString("N", System.Globalization.CultureInfo.InvariantCulture);
}
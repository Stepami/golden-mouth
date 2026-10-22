using Reki.Domain.Common;
using Reki.Domain.Posts;
using Vogen;

namespace Reki.Domain.Worlds;

[ValueObject<string>(comparison: ComparisonGeneration.Omit)]
public readonly partial struct StarId
{
    /// <summary>Преобразует идентификатор звезды в идентификатор автора.</summary>
    /// <returns>Идентификатор автора с типом звезды.</returns>
    public AuthorId ToAuthorId() => AuthorId.From(this);

    private static string NormalizeInput(string value) => value.Trim().ToLowerInvariant();

    private static Validation Validate(string value) => IdentifierValidation.String(value);
}

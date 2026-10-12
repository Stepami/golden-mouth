using Vogen;

namespace Reki.Domain.Posts;

/// <summary>Составной идентификатор автора: тип и идентификатор внутри этого типа.</summary>
[ValueObject<ValueTuple<AuthorKind, string>>(comparison: ComparisonGeneration.Omit, conversions: Conversions.None)]
public readonly partial struct AuthorId
{
    private static Validation Validate((AuthorKind Kind, string Id) value) =>
        value.Kind is AuthorKind.Player or AuthorKind.Star && !string.IsNullOrWhiteSpace(value.Id)
            ? Validation.Ok
            : Validation.Invalid("Author kind and identifier are required.");
}
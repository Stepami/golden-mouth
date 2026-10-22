using Vogen;

namespace Reki.Domain.Posts;

[ValueObject<string>(comparison: ComparisonGeneration.Omit)]
public readonly partial struct CommentId
{
    public static CommentId New() => From(Guid.NewGuid().ToString("N"));

    private static Validation Validate(string value) => IdentifierValidation.String(value);
}

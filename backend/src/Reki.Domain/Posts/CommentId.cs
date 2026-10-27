using Vogen;

namespace Reki.Domain.Posts;

[ValueObject<Guid>(comparison: ComparisonGeneration.Omit)]
public readonly partial struct CommentId
{
    public static CommentId New() => From(Guid.NewGuid());

    public static CommentId From(string value) => From(Guid.Parse(value));

    public override string ToString() => ToString("N", System.Globalization.CultureInfo.InvariantCulture);
}
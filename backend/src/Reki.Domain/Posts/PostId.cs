using Vogen;

namespace Reki.Domain.Posts;

[ValueObject<Guid>(comparison: ComparisonGeneration.Omit)]
public readonly partial struct PostId
{
    public static PostId New() => New(TimeProvider.System.GetUtcNow());

    public static PostId New(DateTimeOffset createdAt) => From(Guid.CreateVersion7(createdAt));

    public static PostId From(string value) => From(Guid.Parse(value));

    public override string ToString() => ToString("N", System.Globalization.CultureInfo.InvariantCulture);
}

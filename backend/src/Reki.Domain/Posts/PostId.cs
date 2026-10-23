using System.Buffers.Binary;
using Vogen;

namespace Reki.Domain.Posts;

[ValueObject<Guid>(comparison: ComparisonGeneration.Omit)]
public readonly partial struct PostId
{
    public static PostId New() => New(TimeProvider.System.GetUtcNow());

    public static PostId New(DateTimeOffset createdAt) => From(Guid.CreateVersion7(createdAt));

    public static PostId From(string value) => From(Guid.Parse(value));

    public DateTimeOffset GetTimestamp()
    {
        Span<byte> bytes = stackalloc byte[16];
        Value.TryWriteBytes(bytes, true, out _);
        var milliseconds = (long)(BinaryPrimitives.ReadUInt64BigEndian(bytes) >> 16);
        return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds);
    }

    public override string ToString() => ToString("N", System.Globalization.CultureInfo.InvariantCulture);
}

using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Reki.Domain.Worlds;

namespace Reki.Infrastructure.Mongo;

internal sealed class WorldDifficultySerializer : SerializerBase<WorldDifficulty>
{
    public override WorldDifficulty Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) =>
        context.Reader.ReadString() switch
        {
            "easy" => WorldDifficulty.Easy,
            "normal" => WorldDifficulty.Normal,
            "hard" => WorldDifficulty.Hard,
            _ => throw new InvalidOperationException("Invalid world difficulty."),
        };

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, WorldDifficulty value) =>
        context.Writer.WriteString(value switch
        {
            WorldDifficulty.Easy => "easy",
            WorldDifficulty.Normal => "normal",
            WorldDifficulty.Hard => "hard",
            _ => throw new InvalidOperationException("Invalid world difficulty."),
        });
}

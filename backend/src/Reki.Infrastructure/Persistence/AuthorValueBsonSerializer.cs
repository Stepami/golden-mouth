using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Reki.Domain.Posts;

namespace Reki.Infrastructure.Persistence;

internal sealed class AuthorValueBsonSerializer : SerializerBase<(AuthorKind Kind, string Id)>
{
    public override (AuthorKind Kind, string Id) Deserialize(
        BsonDeserializationContext context,
        BsonDeserializationArgs args)
    {
        var document = BsonDocumentSerializer.Instance.Deserialize(context, args);
        return ((AuthorKind)document["Kind"].AsInt32, document["Id"].AsString);
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, (AuthorKind Kind, string Id) value)
    {
        var writer = context.Writer;
        writer.WriteStartDocument();
        writer.WriteName("Kind");
        writer.WriteInt32((int)value.Kind);
        writer.WriteName("Id");
        writer.WriteString(value.Id);
        writer.WriteEndDocument();
    }
}
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Reki.Domain.Events;
using Reki.Domain.Invites;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Infrastructure.Mongo;

internal static class MongoClassMappings
{
    /// <summary>
    /// BSON mappings must be registered once before any infrastructure code uses them.
    /// </summary>
    public static void Register()
    {
        BsonSerializer.TryRegisterSerializer(new EnumSerializer<WorldDifficulty>(BsonType.String));
        BsonSerializer.TryRegisterSerializer(new EnumSerializer<AddressType>(BsonType.String));
        BsonSerializer.TryRegisterSerializer(new BsonValueObjectConversions.AddressBsonSerializer());
        BsonSerializer.TryRegisterSerializer(new BsonValueObjectConversions.AuthorIdBsonSerializer());
        BsonSerializer.TryRegisterSerializer(new BsonValueObjectConversions.CommentIdBsonSerializer());
        BsonSerializer.TryRegisterSerializer(
            new BsonValueObjectConversions.RekiEventIdBsonSerializer());
        BsonSerializer.TryRegisterSerializer(
            new BsonValueObjectConversions.InvitationIdBsonSerializer());
        BsonSerializer.TryRegisterSerializer(
            new BsonValueObjectConversions.PlayerIdBsonSerializer());
        BsonSerializer.TryRegisterSerializer(
            new BsonValueObjectConversions.PostIdBsonSerializer());
        BsonSerializer.TryRegisterSerializer(
            new BsonValueObjectConversions.SessionIdBsonSerializer());
        BsonSerializer.TryRegisterSerializer(
            new BsonValueObjectConversions.SkillIdBsonSerializer());
        BsonSerializer.TryRegisterSerializer(
            new BsonValueObjectConversions.StarIdBsonSerializer());
        BsonSerializer.TryRegisterSerializer(
            new BsonValueObjectConversions.ExternalUserIdBsonSerializer());
        BsonSerializer.TryRegisterSerializer(
            new BsonValueObjectConversions.WorldIdBsonSerializer());
        RegisterWithMongoId<PlayerProfile, PlayerId>(item => item.Id);
        RegisterWithMongoId<GamePost, PostId>(item => item.Id, ignoreExtraElements: true);
        RegisterWithMongoId<DailyGameEvent, RekiEventId>(item => item.Id);
        RegisterWithMongoId<World, WorldId>(item => item.Id);
        RegisterWithMongoId<Invitation, InvitationId>(item => item.Id);
    }

    private static void RegisterWithMongoId<TDocument, TId>(
        Expression<Func<TDocument, TId>> idExpression,
        bool ignoreExtraElements = false)
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(TDocument)))
        {
            return;
        }

        BsonClassMap.RegisterClassMap<TDocument>(classMap =>
        {
            classMap.AutoMap();
            classMap.MapIdMember(idExpression);
            classMap.SetIgnoreExtraElements(ignoreExtraElements);
        });
    }
}

using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using MongoDB.Bson.Serialization;
using Reki.Application.Posts;
using Reki.Domain.Events;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Social;
using Reki.Domain.Worlds;

namespace Reki.Infrastructure.Persistence;

internal static class MongoClassMappings
{
    // BSON mappings must be registered once before any infrastructure code uses them.
#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    public static void Register()
    {
        BsonSerializer.TryRegisterSerializer(
            new BsonValueObjectConversions.ChallengeIdBsonSerializer());
        BsonSerializer.TryRegisterSerializer(
            new BsonValueObjectConversions.CommentIdBsonSerializer());
        BsonSerializer.TryRegisterSerializer(
            new BsonValueObjectConversions.EventIdBsonSerializer());
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
            new BsonValueObjectConversions.StarConversationIdBsonSerializer());
        BsonSerializer.TryRegisterSerializer(
            new BsonValueObjectConversions.TelegramUserIdBsonSerializer());
        BsonSerializer.TryRegisterSerializer(
            new BsonValueObjectConversions.WorldIdBsonSerializer());
        RegisterWithMongoId<PlayerProfile, PlayerId>(item => item.Id);
        RegisterWithMongoId<GamePost, PostId>(item => item.Id);
        RegisterWithMongoId<Publication, string>(item => item.Id);
        RegisterWithMongoId<StarConversation, StarConversationId>(item => item.Id);
        RegisterWithMongoId<DailyGameEvent, EventId>(item => item.Id);
        RegisterWithMongoId<GameChallenge, ChallengeId>(item => item.Id);
        RegisterWithMongoId<World, WorldId>(item => item.Id);
    }

    private static void RegisterWithMongoId<TDocument, TId>(
        Expression<Func<TDocument, TId>> idExpression)
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(TDocument)))
        {
            return;
        }

        BsonClassMap.RegisterClassMap<TDocument>(classMap =>
        {
            classMap.AutoMap();
            classMap.MapIdMember(idExpression);
        });
    }
}
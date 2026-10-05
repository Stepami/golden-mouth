using System.Linq.Expressions;
using MongoDB.Bson.Serialization;
using Reki.Domain.Social;
using Reki.Domain.Worlds;

namespace Reki.Infrastructure.Persistence;

internal static class MongoClassMappings
{
    public static void Register()
    {
        BsonSerializer.TryRegisterSerializer(
            new BsonValueObjectConversions.StarConversationIdBsonSerializer());
        BsonSerializer.TryRegisterSerializer(
            new BsonValueObjectConversions.WorldRulesIdBsonSerializer());
        RegisterWithMongoId<StarConversation, StarConversationId>(item => item.Id);
        RegisterWithMongoId<WorldRules, WorldRulesId>(item => item.Id);
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

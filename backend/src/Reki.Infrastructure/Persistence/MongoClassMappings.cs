using MongoDB.Bson.Serialization;
using Reki.Domain.Social;
using Reki.Domain.Worlds;

namespace Reki.Infrastructure.Persistence;

internal static class MongoClassMappings
{
    public static void Register()
    {
        RegisterIgnoringMongoId<StarConversation>();
        RegisterIgnoringMongoId<WorldRules>();
    }

    private static void RegisterIgnoringMongoId<T>()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(T)))
        {
            return;
        }

        BsonClassMap.RegisterClassMap<T>(classMap =>
        {
            classMap.AutoMap();
            classMap.SetIgnoreExtraElements(true);
        });
    }
}
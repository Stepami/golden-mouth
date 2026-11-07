using System.Linq.Expressions;
using MongoDB.Driver;

namespace Reki.Infrastructure.Mongo;

internal static class MongoCollectionExtensions
{
    public static Task ReplaceOneAsync<T>(
        this IMongoCollection<T> collection,
        MongoSessionProvider sessionProvider,
        Expression<Func<T, bool>> filter,
        T replacement,
        CancellationToken cancellationToken) =>
        sessionProvider.Current is { } session
            ? collection.ReplaceOneAsync(session, filter, replacement, cancellationToken: cancellationToken)
            : collection.ReplaceOneAsync(filter, replacement, cancellationToken: cancellationToken);
}

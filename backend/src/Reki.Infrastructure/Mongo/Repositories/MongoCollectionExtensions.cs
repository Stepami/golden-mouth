using System.Linq.Expressions;
using MongoDB.Driver;
using Reki.Infrastructure.Mongo.UnitOfWork;

namespace Reki.Infrastructure.Mongo.Repositories;

internal static class MongoCollectionExtensions
{
    public static Task ReplaceOneAsync<T>(
        this IMongoCollection<T> collection,
        MongoSessionProvider sessionProvider,
        Expression<Func<T, bool>> filter,
        T replacement,
        CancellationToken cancellationToken,
        ReplaceOptions? options = null) =>
        sessionProvider.Current is { } session
            ? collection.ReplaceOneAsync(session, filter, replacement, options, cancellationToken)
            : collection.ReplaceOneAsync(filter, replacement, options, cancellationToken);
}
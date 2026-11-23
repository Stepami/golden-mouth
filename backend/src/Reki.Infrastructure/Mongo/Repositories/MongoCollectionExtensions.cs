using System.Linq.Expressions;
using MongoDB.Driver;
using Reki.Infrastructure.Mongo.UnitOfWork;

namespace Reki.Infrastructure.Mongo.Repositories;

internal static class MongoCollectionExtensions
{
    public static IFindFluent<T, T> Find<T>(
        this IMongoCollection<T> collection,
        MongoSessionProvider sessionProvider,
        Expression<Func<T, bool>> filter) =>
        sessionProvider.Current is { } session
            ? collection.Find(session, filter)
            : collection.Find(filter);

    public static Task InsertOneAsync<T>(
        this IMongoCollection<T> collection,
        MongoSessionProvider sessionProvider,
        T document,
        CancellationToken cancellationToken) =>
        sessionProvider.Current is { } session
            ? collection.InsertOneAsync(session, document, cancellationToken: cancellationToken)
            : collection.InsertOneAsync(document, cancellationToken: cancellationToken);

    public static Task<UpdateResult> UpdateOneAsync<T>(
        this IMongoCollection<T> collection,
        MongoSessionProvider sessionProvider,
        FilterDefinition<T> filter,
        UpdateDefinition<T> update,
        CancellationToken cancellationToken) =>
        sessionProvider.Current is { } session
            ? collection.UpdateOneAsync(session, filter, update, cancellationToken: cancellationToken)
            : collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);

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

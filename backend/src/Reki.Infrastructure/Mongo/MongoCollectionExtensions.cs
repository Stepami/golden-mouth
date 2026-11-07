using System.Linq.Expressions;
using MongoDB.Driver;

namespace Reki.Infrastructure.Mongo;

internal static class MongoCollectionExtensions
{
    public static Task InsertOneAsync<T>(
        this IMongoCollection<T> collection,
        MongoSessionProvider sessionProvider,
        T document,
        CancellationToken cancellationToken) =>
        sessionProvider.Current is { } session
            ? collection.InsertOneAsync(session, document, cancellationToken: cancellationToken)
            : collection.InsertOneAsync(document, cancellationToken: cancellationToken);

    public static IFindFluent<T, T> Find<T>(
        this IMongoCollection<T> collection,
        MongoSessionProvider sessionProvider,
        FilterDefinition<T> filter) =>
        sessionProvider.Current is { } session
            ? collection.Find(session, filter)
            : collection.Find(filter);

    public static IFindFluent<T, T> Find<T>(
        this IMongoCollection<T> collection,
        MongoSessionProvider sessionProvider,
        Expression<Func<T, bool>> filter) =>
        collection.Find(sessionProvider, new ExpressionFilterDefinition<T>(filter));

    public static Task<UpdateResult> UpdateOneAsync<T>(
        this IMongoCollection<T> collection,
        MongoSessionProvider sessionProvider,
        FilterDefinition<T> filter,
        UpdateDefinition<T> update,
        CancellationToken cancellationToken) =>
        sessionProvider.Current is { } session
            ? collection.UpdateOneAsync(session, filter, update, cancellationToken: cancellationToken)
            : collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);

    public static Task<UpdateResult> UpdateOneAsync<T>(
        this IMongoCollection<T> collection,
        MongoSessionProvider sessionProvider,
        Expression<Func<T, bool>> filter,
        UpdateDefinition<T> update,
        CancellationToken cancellationToken) =>
        collection.UpdateOneAsync(
            sessionProvider, new ExpressionFilterDefinition<T>(filter), update, cancellationToken);

    public static Task<UpdateResult> UpdateManyAsync<T>(
        this IMongoCollection<T> collection,
        MongoSessionProvider sessionProvider,
        FilterDefinition<T> filter,
        UpdateDefinition<T> update,
        CancellationToken cancellationToken) =>
        sessionProvider.Current is { } session
            ? collection.UpdateManyAsync(session, filter, update, cancellationToken: cancellationToken)
            : collection.UpdateManyAsync(filter, update, cancellationToken: cancellationToken);

    public static Task<ReplaceOneResult> ReplaceOneAsync<T>(
        this IMongoCollection<T> collection,
        MongoSessionProvider sessionProvider,
        Expression<Func<T, bool>> filter,
        T replacement,
        CancellationToken cancellationToken) =>
        sessionProvider.Current is { } session
            ? collection.ReplaceOneAsync(session, filter, replacement, cancellationToken: cancellationToken)
            : collection.ReplaceOneAsync(filter, replacement, cancellationToken: cancellationToken);
}

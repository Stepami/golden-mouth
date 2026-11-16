using MongoDB.Driver;
using Reki.Application;
using Reki.Application.Exceptions;

namespace Reki.Infrastructure.Mongo;

internal sealed class MongoUnitOfWork(
    IMongoClient client,
    MongoSessionProvider sessionProvider) : IUnitOfWork
{
    public Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken) =>
        ExecuteAsync(operation, static _ => { }, cancellationToken);

    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        Action<T> onSuccess,
        CancellationToken cancellationToken)
    {
        if (sessionProvider.Current is not null)
        {
            throw new InvalidOperationException("Nested units of work are not supported.");
        }

        using var session = await client.StartSessionAsync(cancellationToken: cancellationToken);
        T result;
        try
        {
            result = await session.WithTransactionAsync(
                async (transactionSession, token) =>
                {
                    sessionProvider.Current = transactionSession;
                    try
                    {
                        return await operation(token);
                    }
                    finally
                    {
                        sessionProvider.Current = null;
                    }
                },
                cancellationToken: cancellationToken);
        }
        catch (MongoException exception) when (exception.HasErrorLabel("TransientTransactionError"))
        {
            throw new UnitOfWorkException("The transaction could not be completed.", exception);
        }

        onSuccess(result);
        return result;
    }
}

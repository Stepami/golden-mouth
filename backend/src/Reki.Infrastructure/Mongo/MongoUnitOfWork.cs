using MongoDB.Driver;
using Reki.Application;
using Reki.Application.Exceptions;

namespace Reki.Infrastructure.Mongo;

internal sealed class MongoUnitOfWork(
    IMongoClient client,
    MongoSessionProvider sessionProvider) : IUnitOfWork
{
    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        if (sessionProvider.Current is not null)
        {
            throw new InvalidOperationException("Nested units of work are not supported.");
        }

        using var session = await client.StartSessionAsync(cancellationToken: cancellationToken);
        try
        {
            return await session.WithTransactionAsync(
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
        catch (MongoException exception) when (exception.HasErrorLabel("TransientTransactionError") ||
                                                exception is MongoWriteException { WriteError.Category: ServerErrorCategory.DuplicateKey })
        {
            throw new UnitOfWorkException("The transaction could not be completed.", exception);
        }
    }
}
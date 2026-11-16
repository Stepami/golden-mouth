namespace Reki.Application;

public interface IUnitOfWork
{
    Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken);

    Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        Action<T> onSuccess,
        CancellationToken cancellationToken);
}

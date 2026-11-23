using MongoDB.Driver;

namespace Reki.Infrastructure.Mongo.UnitOfWork;

internal sealed class MongoSessionProvider
{
    private readonly AsyncLocal<IClientSessionHandle?> _current = new();

    public IClientSessionHandle? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }

    public IClientSessionHandle Session =>
        Current ?? throw new InvalidOperationException("An active unit of work is required.");
}
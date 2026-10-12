using Kot.MongoDB.Migrations;
using MongoDB.Driver;

namespace Reki.Infrastructure.Persistence.Migrations;

internal abstract class RekiMigrationBase : IMongoMigration
{
    public abstract DatabaseVersion Version { get; }

    public string Name
    {
        get
        {
            field ??= GetType().Name;
            return field;
        }
    }

    public abstract Task UpAsync(
        IMongoDatabase db,
        IClientSessionHandle session,
        CancellationToken cancellationToken);

    public virtual Task DownAsync(
        IMongoDatabase db,
        IClientSessionHandle session,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
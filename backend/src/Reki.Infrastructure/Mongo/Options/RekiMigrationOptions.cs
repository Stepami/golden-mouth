using Kot.MongoDB.Migrations;
using Microsoft.Extensions.Options;

namespace Reki.Infrastructure.Mongo.Options;

internal sealed class RekiMigrationOptions : MigrationOptions
{
    public RekiMigrationOptions(IOptions<MongoOptions> options)
        : base(options.Value.DatabaseName)
    {
        TransactionScope = TransactionScope.SingleMigration;
        ParallelRunsBehavior = ParallelRunsBehavior.Throw;
    }
}
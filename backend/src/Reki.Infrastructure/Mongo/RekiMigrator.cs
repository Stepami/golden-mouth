using Kot.MongoDB.Migrations;
using Kot.MongoDB.Migrations.Locators;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Reki.Infrastructure.Mongo.Options;

namespace Reki.Infrastructure.Mongo;

internal sealed class RekiMigrator(
    IMigrationsLocator locator,
    IMongoClient client,
    IOptions<MongoOptions> options,
    ILogger<RekiMigrator> logger) : Migrator(locator, client, new RekiMigrationOptions(options), logger);
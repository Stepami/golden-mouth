using Kot.MongoDB.Migrations;
using Kot.MongoDB.Migrations.Locators;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reki.Infrastructure.Mongo.Options;

namespace Reki.Infrastructure.Mongo;

internal sealed class RekiMigrator(
    IMigrationsLocator locator,
    MongoCompositionRoot compositionRoot,
    IOptions<MongoOptions> options,
    ILogger<RekiMigrator> logger) : Migrator(locator, compositionRoot.MongoClient, new RekiMigrationOptions(options), logger);
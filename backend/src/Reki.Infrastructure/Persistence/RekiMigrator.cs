using Kot.MongoDB.Migrations;
using Kot.MongoDB.Migrations.Locators;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Reki.Infrastructure.Persistence;

internal sealed class RekiMigrator(
    IMigrationsLocator locator,
    MongoCompositionRoot compositionRoot,
    IOptions<MongoOptions> options,
    ILogger<RekiMigrator> logger) : Migrator(locator, compositionRoot.MongoClient, new RekiMigrationOptions(options), logger);
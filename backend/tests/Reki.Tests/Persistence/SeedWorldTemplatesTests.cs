using MongoDB.Driver;
using Reki.Domain.Player;
using Reki.Domain.Worlds;
using Reki.Infrastructure.Persistence;
using Reki.Infrastructure.Persistence.Migrations;

namespace Reki.Tests.Persistence;

[ClassDataSource<TestSessionFixture>(Shared = SharedType.PerTestSession)]
[ParallelGroup("Migrations", Order = 0)]
public sealed class SeedWorldTemplatesTests(TestSessionFixture mongo)
{
    [Test]
    public async Task Up_EmptyDatabase_InsertsCompleteDomainTemplatesAsync()
    {
        var database = mongo.MongoClient.GetDatabase($"world_seeds_{Guid.NewGuid():N}");
        try
        {
            using var session = await mongo.MongoClient.StartSessionAsync();
            await new SeedWorldTemplates().UpAsync(database, session, CancellationToken.None);
            var templates = await database.GetCollection<World>(MongoCollections.WorldTemplates)
                .Find(FilterDefinition<World>.Empty).ToListAsync();
            await Assert.That(templates.Select(world => world.Id.Value)).IsEquivalentTo(["stage", "academy", "it", "stream", "own"]);
            await Assert.That(templates.All(world => world.Player.PlayerId.Value == "00000000000000000000000000000000")).IsTrue();
            await Assert.That(templates.All(world => world.Stars.Count > 0 && world.Roles.Count > 0 && world.Rules.Count > 0)).IsTrue();
            await Assert.That(templates.All(world => world.Player.Wants.Count == 3 && world.Player.Achievements.Count == 12)).IsTrue();
            await Assert.That(templates.SelectMany(world => world.Stars).All(star => Enum.IsDefined(star.Trait))).IsTrue();
            await Assert.That(templates.All(world => !string.IsNullOrWhiteSpace(world.Interface.AvatarStyle))).IsTrue();
        }
        finally
        {
            await mongo.MongoClient.DropDatabaseAsync(database.DatabaseNamespace.DatabaseName);
        }
    }

    [Test]
    public async Task Up_RepeatedRun_PreservesExistingDocumentsAsync()
    {
        var database = mongo.MongoClient.GetDatabase($"world_seeds_{Guid.NewGuid():N}");
        try
        {
            using var session = await mongo.MongoClient.StartSessionAsync();
            var migration = new SeedWorldTemplates();
            await migration.UpAsync(database, session, CancellationToken.None);
            var templates = database.GetCollection<World>(MongoCollections.WorldTemplates);
            var stageId = WorldId.From("stage");
            var template = await templates.Find(world => world.Id == stageId).SingleAsync();
            var edited = template with { Interface = template.Interface with { Title = "Обновлённый шаблон" } };
            await templates.ReplaceOneAsync(world => world.Id == stageId, edited);
            var owned = template.Instantiate(PlayerId.From("seed-player"), DateTimeOffset.UnixEpoch) with
            {
                Player = template.Player with { PlayerId = PlayerId.From("seed-player"), Subscribers = 987 },
                Stars = [template.Stars[0] with { Name = "Своя звезда", Relation = 3 }],
            };
            await database.GetCollection<World>(MongoCollections.Worlds).InsertOneAsync(owned);

            await migration.UpAsync(database, session, CancellationToken.None);

            var savedTemplate = await templates.Find(world => world.Id == stageId).SingleAsync();
            var savedWorld = await database.GetCollection<World>(MongoCollections.Worlds).Find(world => world.Id == owned.Id).SingleAsync();
            await Assert.That(await templates.CountDocumentsAsync(FilterDefinition<World>.Empty)).IsEqualTo(5L);
            await Assert.That(savedTemplate.Interface.Title).IsEqualTo(edited.Interface.Title);
            await Assert.That(savedWorld.Player.Subscribers).IsEqualTo(987L);
            await Assert.That(savedWorld.Stars.Single().Name).IsEqualTo("Своя звезда");
            await Assert.That(savedWorld.Stars.Single().Relation).IsEqualTo(3);
        }
        finally
        {
            await mongo.MongoClient.DropDatabaseAsync(database.DatabaseNamespace.DatabaseName);
        }
    }
}

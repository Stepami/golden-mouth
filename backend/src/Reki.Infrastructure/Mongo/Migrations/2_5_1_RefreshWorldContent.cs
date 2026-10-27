using Kot.MongoDB.Migrations;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Reki.Domain.Player;
using Reki.Domain.Worlds;
using Reki.Infrastructure.Mongo.Repositories;

namespace Reki.Infrastructure.Mongo.Migrations;

internal sealed partial class RefreshWorldContent(ILogger<RefreshWorldContent> logger) : RekiMigrationBase
{
    private const string PersonaMarker = "\nПерсона игрока: ";

    public override DatabaseVersion Version => new(2, 5, 1);

    public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        var content = CreateContent().ToDictionary(item => item.Id);
        var owner = PlayerId.From(Guid.Empty);
        var templates = Builders<World>.Filter.In(world => world.Id, content.Keys) &
                        Builders<World>.Filter.Where(world => world.Player.PlayerId == owner && world.TemplateId == null && !world.IsArchived);
        var instances = Builders<World>.Filter.In(world => world.TemplateId, content.Keys.Select(id => (WorldId?)id)) &
                        Builders<World>.Filter.Where(world => world.Player.PlayerId != owner && !world.IsArchived);
        await RefreshCollectionAsync(db.GetCollection<World>(MongoCollections.WorldTemplates), session, templates, content, cancellationToken);
        await RefreshCollectionAsync(db.GetCollection<World>(MongoCollections.Worlds), session, instances, content, cancellationToken);
    }

    private static World ApplyContent(World world, WorldContent content) => world with
    {
        Interface = world.Interface with
        {
            Description = PreservePersona(world.Interface.Description, content.Interface.Description),
            Where = content.Interface.Where,
            Summary = content.Interface.Summary,
            FirstContext = content.Interface.FirstContext,
            FirstPrompt = content.Interface.FirstPrompt,
        },
        Roles = content.Roles,
        Voices = content.Voices,
        PostIdeas = content.PostIdeas,
        DemoPosts = content.DemoPosts,
        Rules = content.Rules,
        Stars = world.Stars.Select(star => UpdateStar(star, content.Stars)).ToArray(),
    };

    private static WorldStar UpdateStar(WorldStar star, IReadOnlyList<StarContent> content)
    {
        var updated = content.FirstOrDefault(item => item.Id == star.Id);
        return updated is null ? star : star with { Role = updated.Role, About = updated.About, Voice = updated.Voice };
    }

    private static string PreservePersona(string previous, string description)
    {
        var marker = previous.IndexOf(PersonaMarker, StringComparison.Ordinal);
        return marker < 0 ? description : description + previous[marker..];
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "World content migration attempt: {Collection}, updated {Count}")]
    private static partial void LogContentAttempt(ILogger logger, string collection, int count);

    private async Task RefreshCollectionAsync(IMongoCollection<World> collection, IClientSessionHandle session, FilterDefinition<World> filter, IReadOnlyDictionary<WorldId, WorldContent> content, CancellationToken cancellationToken)
    {
        var count = 0;
        using var cursor = await collection.FindAsync(session, filter, cancellationToken: cancellationToken);
        await cursor.ForEachAsync(
            async world =>
            {
                var updated = ApplyContent(world, content[world.TemplateId ?? world.Id]);
                await collection.ReplaceOneAsync(session, item => item.Id == world.Id, updated, new ReplaceOptions { IsUpsert = true }, cancellationToken);
                count++;
            },
            cancellationToken);
        LogContentAttempt(logger, collection.CollectionNamespace.CollectionName, count);
    }

    private sealed record WorldContent(
        WorldId Id,
        WorldInterface Interface,
        IReadOnlyList<string> Roles,
        IReadOnlyList<string> Voices,
        IReadOnlyList<string> PostIdeas,
        IReadOnlyList<string> DemoPosts,
        IReadOnlyList<WorldRule> Rules,
        IReadOnlyList<StarContent> Stars);

    private sealed record StarContent(StarId Id, string Role, string About, string Voice);
}

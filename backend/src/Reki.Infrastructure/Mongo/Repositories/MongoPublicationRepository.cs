using MongoDB.Driver;
using Reki.Application.Posts;
using Reki.Domain.Player;

namespace Reki.Infrastructure.Mongo.Repositories;

internal sealed class MongoPublicationRepository(
    IMongoDatabase database,
    MongoSessionProvider sessionProvider) : IPublicationRepository
{
    private readonly IMongoCollection<Publication> _publications =
        database.GetCollection<Publication>(MongoCollections.Publications);

    public async ValueTask<Publication?> GetPublicationAsync(
        string id,
        CancellationToken cancellationToken)
    {
        return await _publications.Find(item => item.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async ValueTask CreatePublicationAsync(
        Publication publication,
        CancellationToken cancellationToken) =>
        await _publications.InsertOneAsync(publication, cancellationToken: cancellationToken);

    public async ValueTask AppendPublicationPartAsync(
        string id,
        PublicationPart part,
        CancellationToken cancellationToken)
    {
        var update = Builders<Publication>.Update.Push(item => item.Parts, part);
        await _publications.UpdateOneAsync(
            item => item.Id == id,
            update,
            cancellationToken: cancellationToken);
    }

    public async ValueTask MarkPublicationReadyAsync(string id, CancellationToken cancellationToken)
    {
        var update = Builders<Publication>.Update
            .Set(item => item.Status, PublicationStatusType.ReadyForModeration)
            .Set(item => item.ExpiresAtUtc, null);
        await _publications.UpdateOneAsync(
            item => item.Id == id,
            update,
            cancellationToken: cancellationToken);
    }

    public async ValueTask MarkPublicationFailedAsync(
        string id,
        DateTime expiresAtUtc,
        PublicationFailure failure,
        CancellationToken cancellationToken)
    {
        var update = Builders<Publication>.Update
            .Set(item => item.Status, PublicationStatusType.Failed)
            .Set(item => item.Failure, failure)
            .Set(item => item.Text, string.Empty)
            .Set(item => item.Parts, [])
            .Set(item => item.ExpiresAtUtc, expiresAtUtc);
        await _publications.UpdateOneAsync(
            item => item.Id == id,
            update,
            cancellationToken: cancellationToken);
    }

    public async ValueTask CompletePublicationAsync(
        string id,
        PlayerId playerId,
        PublishedPost published,
        CancellationToken cancellationToken)
    {
        var filter = Builders<Publication>.Filter.And(
            Builders<Publication>.Filter.Eq(item => item.Id, id),
            Builders<Publication>.Filter.Eq(item => item.PlayerId, playerId));
        var update = Builders<Publication>.Update
            .Set(item => item.Status, PublicationStatusType.Completed)
            .Set(item => item.Result, published)
            .Set(item => item.Text, string.Empty)
            .Set(item => item.Parts, []);
        await _publications.UpdateOneAsync(
            sessionProvider.Session, filter, update, cancellationToken: cancellationToken);
    }

    public async ValueTask DeletePublicationsAsync(PlayerId playerId, CancellationToken cancellationToken) =>
        await _publications.DeleteManyAsync(sessionProvider.Session, item => item.PlayerId == playerId, cancellationToken: cancellationToken);
}

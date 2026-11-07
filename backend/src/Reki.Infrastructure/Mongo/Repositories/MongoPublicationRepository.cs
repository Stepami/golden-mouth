using MongoDB.Driver;
using Reki.Application.Posts;
using Reki.Domain.Player;

namespace Reki.Infrastructure.Mongo.Repositories;

internal sealed class MongoPublicationRepository : IPublicationRepository
{
    private readonly IMongoCollection<Publication> _publications;
    private readonly MongoSessionProvider _sessionProvider;

    public MongoPublicationRepository(IMongoDatabase database, MongoSessionProvider sessionProvider)
    {
        _publications = database.GetCollection<Publication>(MongoCollections.Publications);
        _sessionProvider = sessionProvider;
    }

    public async ValueTask<Publication?> GetPublicationAsync(
        string id,
        CancellationToken cancellationToken)
    {
        return await _publications.Find(item => item.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async ValueTask<bool> TryCreatePublicationAsync(
        Publication publication,
        CancellationToken cancellationToken)
    {
        try
        {
            await _publications.InsertOneAsync(_sessionProvider, publication, cancellationToken);
            return true;
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }

    public async ValueTask AppendPublicationPartAsync(
        string id,
        PublicationPart part,
        CancellationToken cancellationToken)
    {
        var update = Builders<Publication>.Update.Push(item => item.Parts, part);
        var result = await _publications.UpdateOneAsync(
            item => item.Id == id && item.Status == PublicationStatusType.Pending,
            update,
            cancellationToken: cancellationToken);
        if (result.ModifiedCount != 1)
        {
            throw new InvalidOperationException("The publication is no longer pending.");
        }
    }

    public async ValueTask MarkPublicationReadyAsync(string id, CancellationToken cancellationToken)
    {
        var update = Builders<Publication>.Update
            .Set(item => item.Status, PublicationStatusType.ReadyForModeration)
            .Set(item => item.ExpiresAtUtc, null);
        var result = await _publications.UpdateOneAsync(
            item => item.Id == id && item.Status == PublicationStatusType.Pending,
            update,
            cancellationToken: cancellationToken);
        if (result.ModifiedCount != 1)
        {
            throw new InvalidOperationException("The publication could not be marked ready.");
        }
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
            item => item.Id == id && item.Status != PublicationStatusType.Completed,
            update,
            cancellationToken: cancellationToken);
    }

    public async ValueTask<bool> TryCompletePublicationAsync(
        string id,
        PlayerId playerId,
        PublishedPost published,
        CancellationToken cancellationToken)
    {
        var filter = Builders<Publication>.Filter.And(
            Builders<Publication>.Filter.Eq(item => item.Id, id),
            Builders<Publication>.Filter.Eq(item => item.PlayerId, playerId),
            Builders<Publication>.Filter.Eq(item => item.Status, PublicationStatusType.ReadyForModeration));
        var update = Builders<Publication>.Update
            .Set(item => item.Status, PublicationStatusType.Completed)
            .Set(item => item.Result, published)
            .Set(item => item.Text, string.Empty)
            .Set(item => item.Parts, []);
        var result = await _publications.UpdateOneAsync(
            _sessionProvider.Session, filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount == 1;
    }

    public async ValueTask DeletePublicationsAsync(PlayerId playerId, CancellationToken cancellationToken) =>
        await _publications.DeleteManyAsync(_sessionProvider.Session, item => item.PlayerId == playerId, cancellationToken: cancellationToken);
}

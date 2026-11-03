using MongoDB.Driver;
using Reki.Application.Posts;

namespace Reki.Infrastructure.Mongo.Repositories;

internal sealed class MongoPublicationRepository : IPublicationRepository
{
    private readonly IMongoCollection<Publication> _publications;

    public MongoPublicationRepository(IMongoDatabase database)
    {
        _publications = database.GetCollection<Publication>(MongoCollections.Publications);
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
            await _publications.InsertOneAsync(publication, cancellationToken: cancellationToken);
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
}

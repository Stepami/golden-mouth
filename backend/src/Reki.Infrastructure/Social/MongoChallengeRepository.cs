using MongoDB.Driver;
using Reki.Application.Social;
using Reki.Domain.Player;
using Reki.Domain.Social;
using Reki.Infrastructure.Persistence;

namespace Reki.Infrastructure.Social;

internal sealed class MongoChallengeRepository : IChallengeRepository
{
    private readonly IMongoCollection<GameChallenge> _challenges;

    public MongoChallengeRepository(MongoCompositionRoot compositionRoot)
    {
        _challenges = compositionRoot.MongoDatabase.GetCollection<GameChallenge>(MongoCollections.Challenges);
    }

    public async ValueTask<GameChallenge?> GetActiveChallengeAsync(
        PlayerId playerId,
        CancellationToken cancellationToken)
    {
        var participantFilter = Builders<GameChallenge>.Filter
            .AnyEq(item => item.Participants, playerId);
        var statusFilter = Builders<GameChallenge>.Filter
            .Eq(item => item.Status, ChallengeStatus.Active);
        return await _challenges
            .Find(Builders<GameChallenge>.Filter.And(participantFilter, statusFilter))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async ValueTask<GameChallenge?> GetPendingChallengeAsync(
        PlayerId firstPlayerId,
        PlayerId secondPlayerId,
        CancellationToken cancellationToken)
    {
        var participantsFilter = Builders<GameChallenge>.Filter.And(
            Builders<GameChallenge>.Filter.AnyEq(item => item.Participants, firstPlayerId),
            Builders<GameChallenge>.Filter.AnyEq(item => item.Participants, secondPlayerId));
        var statusFilter = Builders<GameChallenge>.Filter
            .Eq(item => item.Status, ChallengeStatus.Pending);
        return await _challenges
            .Find(Builders<GameChallenge>.Filter.And(participantsFilter, statusFilter))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async ValueTask<bool> TryInsertChallengeAsync(
        GameChallenge challenge,
        CancellationToken cancellationToken)
    {
        try
        {
            await _challenges.InsertOneAsync(challenge, cancellationToken: cancellationToken);
            return true;
        }
        catch (MongoWriteException exception) when (
            exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }

    public async ValueTask<bool> TryReplaceChallengeAsync(
        GameChallenge challenge,
        int expectedVersion,
        CancellationToken cancellationToken)
    {
        var idFilter = Builders<GameChallenge>.Filter
            .Eq(item => item.Id, challenge.Id);
        var versionFilter = Builders<GameChallenge>.Filter
            .Eq(item => item.Version, expectedVersion);
        try
        {
            var result = await _challenges.ReplaceOneAsync(
                Builders<GameChallenge>.Filter.And(idFilter, versionFilter),
                challenge,
                cancellationToken: cancellationToken);
            return result.ModifiedCount == 1;
        }
        catch (MongoWriteException exception) when (
            exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }
}

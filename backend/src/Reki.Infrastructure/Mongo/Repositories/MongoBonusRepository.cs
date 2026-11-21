using MongoDB.Driver;
using Reki.Application.Player;
using Reki.Domain.Bonuses;
using Reki.Domain.Player;

namespace Reki.Infrastructure.Mongo.Repositories;

internal sealed class MongoBonusRepository(
    IMongoDatabase database,
    MongoSessionProvider sessionProvider) : IBonusRepository
{
    private readonly IMongoCollection<Bonus> _bonuses = database.GetCollection<Bonus>(MongoCollections.Bonuses);

    public async Task<TBonus?> TryClaimBonus<TBonus>(PlayerId id, CancellationToken ct)
        where TBonus : Bonus =>
        await _bonuses.OfType<TBonus>().FindOneAndUpdateAsync(
            sessionProvider.Session,
            Builders<TBonus>.Filter.Where(bonus => bonus.PlayerId == id && !bonus.IsClaimed),
            Builders<TBonus>.Update.Set(bonus => bonus.IsClaimed, true),
            new FindOneAndUpdateOptions<TBonus>
            {
                Sort = Builders<TBonus>.Sort.Ascending(bonus => bonus.Id),
                ReturnDocument = ReturnDocument.After,
            },
            ct);

    public Task InsertBonusesAsync(IReadOnlyList<Bonus> bonuses, CancellationToken ct) =>
        _bonuses.InsertManyAsync(sessionProvider.Session, bonuses, cancellationToken: ct);

    public Task DeleteForPlayerAsync(PlayerId id, CancellationToken ct) =>
        _bonuses.DeleteManyAsync(sessionProvider.Session, bonus => bonus.PlayerId == id, cancellationToken: ct);
}

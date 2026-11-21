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

#pragma warning disable AV1554 // Match the optional cancellation token in IBonusRepository.
    public async Task<TBonus?> GetActiveBonus<TBonus>(PlayerId id, CancellationToken ct = default)
        where TBonus : Bonus
#pragma warning restore AV1554
    {
        var bonuses = _bonuses.OfType<TBonus>();
        var filter = Builders<TBonus>.Filter.Where(bonus => bonus.PlayerId == id && !bonus.IsClaimed);
        return await (sessionProvider.Current is { } session
                ? bonuses.Find(session, filter)
                : bonuses.Find(filter))
            .SortBy(bonus => bonus.Id)
            .FirstOrDefaultAsync(ct);
    }

    public Task SaveBonus(Bonus bonus, CancellationToken ct) =>
        _bonuses.ReplaceOneAsync(
            sessionProvider,
            item => item.Id == bonus.Id,
            bonus,
            ct,
            new ReplaceOptions { IsUpsert = true });
}

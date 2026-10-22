using MongoDB.Driver;
using Reki.Application.Social;
using Reki.Domain.Player;
using Reki.Domain.Social;
using Reki.Domain.Worlds;
using Reki.Infrastructure.Persistence;

namespace Reki.Infrastructure.Social;

internal sealed class MongoStarConversationRepository : IStarConversationRepository
{
    private readonly IMongoCollection<StarConversation> _starMemories;

    public MongoStarConversationRepository(MongoCompositionRoot compositionRoot)
    {
        _starMemories = compositionRoot.MongoDatabase.GetCollection<StarConversation>(MongoCollections.StarsMemory);
    }

    public async ValueTask<StarConversation?> GetStarConversationAsync(
        PlayerId playerId,
        StarId starId,
        CancellationToken cancellationToken,
        WorldId worldId)
    {
        return await _starMemories
            .Find(item => item.PlayerId == playerId && item.StarId == starId && item.WorldId == worldId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async ValueTask SaveStarConversationAsync(
        StarConversation conversation,
        CancellationToken cancellationToken)
    {
        var options = new ReplaceOptions { IsUpsert = true };
        await _starMemories.ReplaceOneAsync(
            item => item.PlayerId == conversation.PlayerId && item.WorldId == conversation.WorldId &&
                    item.StarId == conversation.StarId,
            conversation,
            options,
            cancellationToken);
    }
}

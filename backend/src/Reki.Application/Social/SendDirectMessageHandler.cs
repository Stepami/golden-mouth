using Mediator;
using Microsoft.Extensions.Options;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Application.Common.Safety;
using Reki.Application.Posts;
using Reki.Application.Worlds;
using Reki.Domain.Player;
using Reki.Domain.Social;
using TgMiniAppAuth.AuthContext.User;

namespace Reki.Application.Social;

public sealed class SendDirectMessageHandler(
    ITelegramUserAccessor telegramUser,
    IGameStore store,
    IOptions<GameBalance> balance,
    GameDay gameDay,
    PlayerReader playerReader,
    StarTurnGenerator turnGenerator) : IRequestHandler<SendDirectMessageCommand, SentDirectMessage>
{
    public async ValueTask<SentDirectMessage> Handle(
        SendDirectMessageCommand request,
        CancellationToken cancellationToken)
    {
        var telegramUserId = TelegramUserId.From(telegramUser.User.Id);
        var player = await playerReader.GetAsync(telegramUserId, cancellationToken);
        _ = StarCatalog.Require(player.World, request.StarId);
        var periodStart = gameDay.PeriodStart;
        var conversation = await store.GetStarConversationAsync(
            player.Id,
            request.StarId,
            cancellationToken) ?? new StarConversation(
                StarConversationId.New(),
                player.Id,
                request.StarId,
                [],
                [],
                0,
                periodStart);
        if (conversation.PeriodStartedAtUtc != periodStart)
        {
            conversation = conversation with { RepliesUsed = 0, PeriodStartedAtUtc = periodStart };
        }

        if (conversation.RepliesUsed >= balance.Value.Relations.DirectMessageRepliesPerDay)
        {
            throw new RequestConflictException("The daily direct-message limit has been reached.");
        }

        var storedRules = await store.GetWorldRulesAsync(
            player.Id,
            player.World,
            cancellationToken);
        var worldRules = player.World.Value == "own" && storedRules is not null
            ? storedRules
            : WorldRulesDefaults.Create(player.Id, player.World);
        var generated = await turnGenerator.GenerateDirectMessageAsync(
            new StarDirectMessageContext(
                new StarAgentProfile(
                    player.World,
                    worldRules.Title,
                    worldRules.Description,
                    worldRules.Rules,
                    player.Settings.Drama,
                    player.Settings.Voice,
                    request.StarId,
                    conversation.Memory,
                    player.Address),
                request.Text,
                conversation.Messages.TakeLast(12).ToArray()),
            player,
            cancellationToken);
        IReadOnlyList<DirectMessage> messages =
        [
            .. conversation.Messages,
            new DirectMessage("me", request.Text),
            new DirectMessage("star", generated.Text),
        ];
        var memory = !MemoryFactPolicy.IsAllowed(generated.MemoryFact) ||
                     conversation.Memory.Contains(generated.MemoryFact, StringComparer.OrdinalIgnoreCase)
            ? conversation.Memory
            : [.. conversation.Memory, generated.MemoryFact];
        var updated = conversation with
        {
            Messages = messages,
            Memory = memory,
            RepliesUsed = conversation.RepliesUsed + 1,
        };
        await store.SaveStarConversationAsync(updated, cancellationToken);
        return new SentDirectMessage(
            generated.Text,
            balance.Value.Relations.DirectMessageRepliesPerDay - updated.RepliesUsed);
    }
}

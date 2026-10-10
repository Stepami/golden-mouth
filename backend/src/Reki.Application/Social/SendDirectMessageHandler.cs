using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Application.Common.Safety;
using Reki.Application.Posts;
using Reki.Application.Worlds;
using Reki.Domain.Player;
using Reki.Domain.Social;
using Reki.Domain.Worlds;
using TgMiniAppAuth.AuthContext.User;

namespace Reki.Application.Social;

public sealed class SendDirectMessageHandler(
    ITelegramUserAccessor telegramUser,
    IGameStore store,
    GameBalance balance,
    GameDay gameDay,
    PlayerReader playerReader,
    StarTurnGenerator turnGenerator) : IRequestHandler<SendDirectMessageCommand, SentDirectMessage>
{
    public async ValueTask<SentDirectMessage> Handle(
        SendDirectMessageCommand request,
        CancellationToken cancellationToken)
    {
        TelegramUserId telegramUserId = TelegramUserId.From(telegramUser.User.Id);
        PlayerProfile player = await playerReader.GetAsync(telegramUserId, cancellationToken);
        _ = StarCatalog.Require(player.World, request.StarId);
        DateTimeOffset periodStart = gameDay.PeriodStart;
        StarConversation conversation = await store.GetStarConversationAsync(
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

        if (conversation.RepliesUsed >= balance.Relations.DirectMessageRepliesPerDay)
        {
            throw new RequestConflictException("The daily direct-message limit has been reached.");
        }

        WorldRules? storedRules = await store.GetWorldRulesAsync(
            player.Id,
            player.World,
            cancellationToken);
        WorldRules worldRules = player.World.Value == "own" && storedRules is not null
            ? storedRules
            : WorldRulesDefaults.Create(player.Id, player.World);
        GeneratedDirectMessage generated = await turnGenerator.GenerateDirectMessageAsync(
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
        IReadOnlyList<string> memory = !MemoryFactPolicy.IsAllowed(generated.MemoryFact) ||
            conversation.Memory.Contains(generated.MemoryFact, StringComparer.OrdinalIgnoreCase)
            ? conversation.Memory
            : [.. conversation.Memory, generated.MemoryFact];
        StarConversation updated = conversation with
        {
            Messages = messages,
            Memory = memory,
            RepliesUsed = conversation.RepliesUsed + 1,
        };
        await store.SaveStarConversationAsync(updated, cancellationToken);
        return new SentDirectMessage(
            generated.Text,
            balance.Relations.DirectMessageRepliesPerDay - updated.RepliesUsed);
    }
}

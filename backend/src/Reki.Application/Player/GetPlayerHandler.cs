using Mediator;
using Microsoft.Extensions.Options;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Domain.Player;
using TgMiniAppAuth.AuthContext.User;

namespace Reki.Application.Player;

public sealed class GetPlayerHandler(
    ITelegramUserAccessor telegramUser,
    IGameStore store,
    GameDay gameDay,
    IOptions<GameBalance> balance) : IRequestHandler<GetPlayerQuery, PlayerProfile>
{
    public async ValueTask<PlayerProfile> Handle(GetPlayerQuery request, CancellationToken cancellationToken)
    {
        var player = await ReadAsync(request, cancellationToken);
        if (!request.NormalizeDay)
        {
            return await WithReferralBenefitsAsync(player, cancellationToken);
        }

        var normalized = gameDay.Normalize(player);
        if (!ReferenceEquals(player, normalized) && !await store.TryReplacePlayerAsync(
                normalized,
                player.Version,
                cancellationToken))
        {
            normalized = await ReadAsync(request, cancellationToken);
        }

        return await WithReferralBenefitsAsync(normalized, cancellationToken);
    }

    private async ValueTask<PlayerProfile> WithReferralBenefitsAsync(
        PlayerProfile player,
        CancellationToken cancellationToken)
    {
        var state = await store.GetInviteStateAsync(player.Id, gameDay.PeriodStart, cancellationToken);
        var inviterBonuses = Math.Min(
            state.InviterBonuses,
            Math.Max(0, balance.Value.InviteBonusMaxPerDay - state.LegacyInviterBonuses));
        return player with
        {
            ReferralBenefits = new(
                (inviterBonuses * balance.Value.InviteBonusBoth) + state.InviteePosts,
                state.PendingFirstPostBonus,
                state.HasAttribution),
        };
    }

    private async ValueTask<PlayerProfile> ReadAsync(GetPlayerQuery request, CancellationToken cancellationToken)
    {
        var player = request.PlayerId is { } playerId
            ? await store.GetPlayerAsync(playerId, cancellationToken)
            : await store.GetPlayerAsync(TelegramUserId.From(telegramUser.User.Id), cancellationToken);
        return player ?? throw new ResourceNotFoundException("Player not found.");
    }
}
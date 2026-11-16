using Mediator;
using Microsoft.Extensions.Options;
using Reki.Application.Balance;
using Reki.Application.Exceptions;
using Reki.Application.Invites;
using Reki.Application.Player.UpdateLastSeenAtCommand;
using Reki.Application.Telemetry;
using Reki.Domain.Invites;
using Reki.Domain.Player;

namespace Reki.Application.Player.OnboardPlayerCommand;

internal sealed class OnboardPlayerCommandHandler(
    IUnitOfWork unitOfWork,
    IInvitationRepository invitationRepository,
    IPlayerRepository playerRepository,
    ISender sender,
    IOptionsMonitor<GameBalance> balance,
    TimeProvider timeProvider,
    ProductTelemetry analytics) : ICommandHandler<OnboardPlayer, PlayerProfile>
{
    public async ValueTask<PlayerProfile> Handle(
        OnboardPlayer command,
        CancellationToken cancellationToken)
    {
        if (await playerRepository.GetPlayerAsync(command.ExternalUserId, cancellationToken) is not null)
        {
            throw new RequestConflictException("Onboarding has already been completed.");
        }

        var currentBalance = balance.CurrentValue;
        var inviter = await ResolveInviterAsync(command, cancellationToken);
        var playerId = PlayerId.New();
        PlayerNotificationChannel[] notificationChannels = command.ExternalUserId.TryGetTelegramId(out var chatId)
            ? [PlayerNotificationChannel.Telegram(chatId.Value)]
            : [];
        var completed = new PlayerProfile(
            playerId,
            [command.ExternalUserId],
            command.Address,
            PlayerWallet.Create(timeProvider.GetUtcNow()),
            new Dictionary<SkillId, SkillProgress>
            {
                [SkillId.Brave] = new(1, 0, 0),
                [SkillId.Humor] = new(1, 0, 0),
                [SkillId.Topic] = new(1, 0, 0),
            },
            command.BirthYear,
            new PlayerSettings("all", "22:00-09:00", false),
            notificationChannels,
            [],
            lastSeenAt: timeProvider.GetUtcNow());

        await unitOfWork.ExecuteAsync(
            async token =>
            {
                await playerRepository.CreatePlayerAsync(completed, token);
                if (inviter is not null)
                {
                    var invite = Invitation.Create(
                        inviter.Id,
                        completed.Id,
                        timeProvider.GetUtcNow(),
                        currentBalance.InviteBonusBoth);
                    await invitationRepository.CreateInviteAsync(invite, token);
                }

                return true;
            },
            cancellationToken);
        await sender.Send(new UpdateLastSeenAt(completed), cancellationToken);

        analytics.TrackPlayer(
            completed,
            "player_created",
            new Dictionary<string, string>(),
            referred: inviter is not null);
        return completed;
    }

    private async ValueTask<PlayerProfile?> ResolveInviterAsync(
        OnboardPlayer request,
        CancellationToken cancellationToken)
    {
        var code = request.ReferralCode;
        if (code is null || !code.StartsWith("ref_", StringComparison.Ordinal) ||
            !Guid.TryParseExact(code[4..], "N", out _))
        {
            return null;
        }

        var inviter = await playerRepository.GetPlayerAsync(PlayerId.From(code[4..]), cancellationToken);
        return inviter;
    }
}

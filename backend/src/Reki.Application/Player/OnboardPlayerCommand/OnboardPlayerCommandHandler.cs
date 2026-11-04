using Mediator;
using Microsoft.Extensions.Options;
using Reki.Application.Balance;
using Reki.Application.Exceptions;
using Reki.Application.Invites;
using Reki.Application.Telemetry;
using Reki.Domain.Invites;
using Reki.Domain.Player;

namespace Reki.Application.Player.OnboardPlayerCommand;

internal sealed class OnboardPlayerCommandHandler(
    IInvitationRepository invitationRepository,
    IPlayerRepository playerRepository,
    GameDayPeriod gameDayPeriod,
    IOptionsMonitor<GameBalance> balance,
    TimeProvider timeProvider,
    ProductTelemetry analytics) : ICommandHandler<OnboardPlayer, PlayerProfile>
{
    public async ValueTask<PlayerProfile> Handle(
        OnboardPlayer request,
        CancellationToken cancellationToken)
    {
        var telegramUserId = request.TelegramUserId;
        if (await playerRepository.GetPlayerAsync(telegramUserId, cancellationToken) is not null)
        {
            throw new RequestConflictException("Onboarding has already been completed.");
        }

        var currentBalance = balance.CurrentValue;
        var inviter = await ResolveInviterAsync(request, telegramUserId, cancellationToken);
        var created = CreatePlayer(request, telegramUserId, gameDayPeriod.PeriodStart, timeProvider.GetUtcNow()) with
        {
            AgeBand = request.AgeBand,
        };
        await playerRepository.CreatePlayerAsync(created, cancellationToken);
        if (inviter is not null)
        {
            var invite = Invitation.Create(inviter.Id, created.Id, timeProvider.GetUtcNow(), currentBalance.InviteBonusBoth);
            await invitationRepository.CreateInviteAsync(invite, cancellationToken);
        }

        analytics.TrackPlayer(created, "player_created", new Dictionary<string, string>(), referred: inviter is not null);
        return created;
    }

    private static PlayerProfile CreatePlayer(
        OnboardPlayer request,
        TelegramUserId telegramUserId,
        DateTimeOffset periodStart,
        DateTimeOffset now)
    {
        var player = new PlayerProfile
        {
            Id = PlayerId.New(),
            Wallet = PlayerWallet.Create(now),
            TelegramUserId = telegramUserId,
            BirthGroup = request.BirthGroup,
            Address = request.Address,
            PostsPeriodStartedAtUtc = periodStart,
            StreakDays = 1,
            Skills = new Dictionary<SkillId, SkillProgress>
            {
                [SkillId.From("brave")] = new(1, 0, 0),
                [SkillId.From("humor")] = new(1, 0, 0),
                [SkillId.From("topic")] = new(1, 0, 0),
            },
            Settings = new PlayerSettings("all", "22:00-09:00", false),
        };
        return player;
    }

    private async ValueTask<PlayerProfile?> ResolveInviterAsync(
        OnboardPlayer request,
        TelegramUserId telegramUserId,
        CancellationToken cancellationToken)
    {
        var code = request.ReferralCode;
        if (code is null || !code.StartsWith("ref_", StringComparison.Ordinal) ||
            !Guid.TryParseExact(code[4..], "N", out _))
        {
            return null;
        }

        var inviter = await playerRepository.GetPlayerAsync(PlayerId.From(code[4..]), cancellationToken);
        return inviter is not null && inviter.TelegramUserId != telegramUserId
            ? inviter
            : null;
    }
}

using Mediator;
using Microsoft.Extensions.Options;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Application.Telemetry;
using Reki.Domain.Invites;
using Reki.Domain.Player;

namespace Reki.Application.Player;

public sealed class OnboardPlayerHandler(
    IGameStore store,
    GameDayPeriod gameDayPeriod,
    IOptions<GameBalance> balance,
    TimeProvider timeProvider,
    ProductTelemetry analytics) : IRequestHandler<OnboardPlayerCommand, PlayerProfile>
{
    public async ValueTask<PlayerProfile> Handle(
        OnboardPlayerCommand request,
        CancellationToken cancellationToken)
    {
        var telegramUserId = request.TelegramUserId;
        if (await store.GetPlayerAsync(telegramUserId, cancellationToken) is not null)
        {
            throw new RequestConflictException("Onboarding has already been completed.");
        }

        var inviter = await ResolveInviterAsync(request, telegramUserId, cancellationToken);
        var created = CreatePlayer(request, telegramUserId, gameDayPeriod.PeriodStart) with
        {
            AgeBand = request.AgeBand,
            Energy = balance.Value.FreePostsPerDay,
        };
        await store.CreatePlayerAsync(created, cancellationToken);
        if (inviter is not null)
        {
            var invite = Invitation.Create(inviter.Id, created.Id, timeProvider.GetUtcNow(), balance.Value.InviteBonusBoth);
            await store.CreateInviteAsync(invite, cancellationToken);
        }

        analytics.TrackPlayer(created, "player_created", new Dictionary<string, string>(), referred: inviter is not null);
        return created;
    }

    private static PlayerProfile CreatePlayer(
        OnboardPlayerCommand request,
        TelegramUserId telegramUserId,
        DateTimeOffset periodStart)
    {
        var player = new PlayerProfile
        {
            Id = PlayerId.New(),
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
        OnboardPlayerCommand request,
        TelegramUserId telegramUserId,
        CancellationToken cancellationToken)
    {
        var code = request.ReferralCode;
        if (code is null || !code.StartsWith("ref_", StringComparison.Ordinal) ||
            !Guid.TryParseExact(code[4..], "N", out _))
        {
            return null;
        }

        var inviter = await store.GetPlayerAsync(PlayerId.From(code[4..]), cancellationToken);
        return inviter is not null && inviter.TelegramUserId != telegramUserId
            ? inviter
            : null;
    }
}

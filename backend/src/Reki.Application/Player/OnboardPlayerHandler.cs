using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Application.Telemetry;
using Reki.Domain.Invites;
using Reki.Domain.Player;
using TgMiniAppAuth.AuthContext.User;

namespace Reki.Application.Player;

public sealed class OnboardPlayerHandler(
    ITelegramUserAccessor telegramUser,
    IGameStore store,
    GameDayPeriod gameDayPeriod,
    TimeProvider timeProvider,
    ProductTelemetry analytics) : IRequestHandler<OnboardPlayerCommand, PlayerProfile>
{
    public async ValueTask<PlayerProfile> Handle(
        OnboardPlayerCommand request,
        CancellationToken cancellationToken)
    {
        var telegramUserId = TelegramUserId.From(telegramUser.User.Id);
        if (await store.GetPlayerAsync(telegramUserId, cancellationToken) is not null)
        {
            throw new RequestConflictException("Onboarding has already been completed.");
        }

        var inviter = await ResolveInviterAsync(request, telegramUserId, cancellationToken);
        var created = CreatePlayer(request, telegramUserId, gameDayPeriod.PeriodStart) with
        {
            AgeBand = request.AgeBand,
        };
        await store.CreatePlayerAsync(created, cancellationToken);
        if (inviter is not null)
        {
            var invite = Invitation.Create(inviter.Id, created.Id, timeProvider.GetUtcNow());
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

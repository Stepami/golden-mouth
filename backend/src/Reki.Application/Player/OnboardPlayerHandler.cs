using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Application.Social;
using Reki.Domain.Player;

namespace Reki.Application.Player;

public sealed class OnboardPlayerHandler(
    IGameStore store,
    GameBalance balance,
    GameDay gameDay,
    PlayerReader playerReader,
    TimeProvider timeProvider) : IRequestHandler<OnboardPlayerCommand, PlayerView>
{
    public async ValueTask<PlayerView> Handle(
        OnboardPlayerCommand request,
        CancellationToken cancellationToken)
    {
        _ = StarCatalog.Require(request.World, request.Star);
        for (int attempt = 0; attempt < 3; attempt++)
        {
            PlayerProfile? existing = await store.GetPlayerAsync(request.TelegramUserId, cancellationToken);
            if (existing is null)
            {
                PlayerProfile? inviter = await ResolveInviterAsync(request, cancellationToken);
                PlayerProfile? updatedInviter = inviter is null
                    ? null
                    : gameDay.Normalize(inviter.EnsureDefaults())
                        .GrantInviteBonus(balance.InviteBonusBoth, balance.InviteBonusMaxPerDay);
                if (ReferenceEquals(updatedInviter, inviter))
                {
                    updatedInviter = null;
                }

                PlayerProfile created = CreatePlayer(request, gameDay.PeriodStart, balance.Growth.StartingSubscribers) with
                {
                    InvitedBy = inviter?.Id,
                    InvitedAtUtc = inviter is null ? null : timeProvider.GetUtcNow(),
                    FirstPostInviteBonus = inviter is null ? 0 : balance.InviteBonusBoth,
                };
                if (await store.TryCreatePlayerAsync(
                    created,
                    updatedInviter,
                    inviter?.Version ?? 0,
                    cancellationToken))
                {
                    return playerReader.CreateView(created);
                }
            }
            else
            {
                PlayerProfile normalized = gameDay.Normalize(existing.EnsureDefaults());
                PlayerProfile updated = UpdatePlayer(normalized, request);
                if (await store.TryReplacePlayerAsync(updated, existing.Version, cancellationToken))
                {
                    return playerReader.CreateView(updated);
                }
            }
        }

        throw new RequestConflictException("Player changed while onboarding. Retry the request.");
    }

    private static PlayerProfile CreatePlayer(
        OnboardPlayerCommand request,
        DateTimeOffset periodStart,
        long startingSubscribers)
    {
        IReadOnlyDictionary<SkillId, SkillProgress> skills = new Dictionary<SkillId, SkillProgress>
        {
            [SkillId.From("brave")] = new(1, 0, 0),
            [SkillId.From("humor")] = new(1, 0, 0),
            [SkillId.From("topic")] = new(1, 0, 0),
        };
        var settings = new PlayerSettings("mid", "all", "22:00-09:00", "default", false);
        return new PlayerProfile(
            PlayerId.New(),
            request.TelegramUserId,
            request.BirthGroup,
            request.Name,
            request.Handle,
            request.Role,
            request.Handle,
            request.Address,
            request.World,
            startingSubscribers,
            0,
            0,
            periodStart,
            false,
            1,
            settings,
            skills,
            null,
            null,
            0,
            DailyWantCatalog.Create(),
            AchievementCatalog.Create(request.World),
            [new StarRelationProgress(request.Star, 0, 0, periodStart)],
            null,
            0,
            0,
            0,
            0,
            0,
            null);
    }

    private static PlayerProfile UpdatePlayer(
        PlayerProfile player,
        OnboardPlayerCommand request)
    {
        IReadOnlyList<AchievementProgress> achievements = player.Achievements.Any(item =>
            item.WorldId == request.World)
            ? player.Achievements
            : [.. player.Achievements, .. AchievementCatalog.Create(request.World)];
        IReadOnlyList<StarRelationProgress> relations = player.StarRelations.Any(item =>
            item.StarId == request.Star)
            ? player.StarRelations
            :
            [
                .. player.StarRelations,
                new StarRelationProgress(request.Star, 0, 0, player.PostsPeriodStartedAtUtc),
            ];

        return player with
        {
            BirthGroup = request.BirthGroup,
            Name = request.Name,
            Handle = request.Handle,
            Role = request.Role,
            Avatar = request.Handle,
            Address = request.Address,
            World = request.World,
            Achievements = achievements,
            StarRelations = relations,
            Version = player.Version + 1,
        };
    }

    private async ValueTask<PlayerProfile?> ResolveInviterAsync(
        OnboardPlayerCommand request,
        CancellationToken cancellationToken)
    {
        string? code = request.ReferralCode;
        if (code is null || !code.StartsWith("ref_", StringComparison.Ordinal) ||
            !Guid.TryParseExact(code[4..], "N", out _))
        {
            return null;
        }

        PlayerProfile? inviter = await store.GetPlayerAsync(PlayerId.From(code[4..]), cancellationToken);
        return inviter is not null && inviter.TelegramUserId != request.TelegramUserId
            ? inviter
            : null;
    }
}

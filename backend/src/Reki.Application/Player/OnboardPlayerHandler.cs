using Mediator;
using Reki.Application.Common;
using Reki.Domain.Player;

namespace Reki.Application.Player;

public sealed class OnboardPlayerHandler(
    IGameStore store,
    GameBalance balance,
    GameDay gameDay,
    PlayerReader playerReader) : IRequestHandler<OnboardPlayerCommand, PlayerView>
{
    public async ValueTask<PlayerView> Handle(
        OnboardPlayerCommand request,
        CancellationToken cancellationToken)
    {
        DateTimeOffset periodStart = gameDay.PeriodStart;
        PlayerProfile? existing = await store.GetPlayerAsync(
            request.TelegramUserId,
            cancellationToken);
        PlayerProfile? normalized = existing is null
            ? null
            : gameDay.Normalize(existing.EnsureDefaults());
        PlayerProfile player = normalized is null
            ? CreatePlayer(request, periodStart, balance.Growth.StartingSubscribers)
            : UpdatePlayer(normalized, request);

        await store.UpsertPlayerAsync(player, cancellationToken);
        return playerReader.CreateView(player);
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
}

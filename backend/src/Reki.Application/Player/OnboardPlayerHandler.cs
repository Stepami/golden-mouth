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
        PlayerProfile? normalized = existing is null ? null : gameDay.Normalize(existing);
        PlayerProfile player = normalized is null
            ? CreatePlayer(request, periodStart, balance.StartingSubscribers)
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
            [SkillId.From("brave")] = new(1, 0),
            [SkillId.From("humor")] = new(1, 0),
            [SkillId.From("topic")] = new(1, 0),
        };

        var settings = new PlayerSettings("mid", "all", "22:00-09:00", "default", false);
        return new PlayerProfile(
            PlayerId.New(),
            request.TelegramUserId,
            request.BirthGroup,
            request.Name,
            request.Handle,
            request.Role,
            request.Avatar,
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
            0);
    }

    private static PlayerProfile UpdatePlayer(
        PlayerProfile player,
        OnboardPlayerCommand request)
    {
        return player with
        {
            BirthGroup = request.BirthGroup,
            Name = request.Name,
            Handle = request.Handle,
            Role = request.Role,
            Avatar = request.Avatar,
            Address = request.Address,
            World = request.World,
            Version = player.Version + 1,
        };
    }
}

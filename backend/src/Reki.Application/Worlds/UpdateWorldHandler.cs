using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Application.Common.Safety;
using Reki.Application.Player;

namespace Reki.Application.Worlds;

public sealed class UpdateWorldHandler(IGameStore store, ISender sender, PlayerSafetyGuard safetyGuard)
    : IRequestHandler<UpdateWorldCommand>
{
    public async ValueTask<Unit> Handle(UpdateWorldCommand request, CancellationToken cancellationToken)
    {
        var player = await sender.Send(new GetPlayerQuery(), cancellationToken);
        var world = await sender.Send(new GetWorldQuery(player.Id, request.WorldId), cancellationToken);
        var definition = request.Definition;
        if (definition.Stars.Count == 0 || definition.Stars.Any(star => !Enum.IsDefined(star.Trait)) ||
            definition.Stars.Select(star => star.Id).Distinct().Count() != definition.Stars.Count)
        {
            throw new ContentRejectedException();
        }

        await safetyGuard.EnsureInputAllowedAsync(
            player,
            WorldSafetyInputs.From(definition),
            cancellationToken);
        var updated = definition with
        {
            Id = world.Id,
            Version = world.Version + 1,
            BalanceKey = world.BalanceKey,
            ArchivedStars = world.ArchivedStars.Concat(world.Stars)
                .Where(star => !definition.Stars.Any(value => value.Id == star.Id)).DistinctBy(star => star.Id).ToArray(),
            Stars = definition.Stars.Select(star =>
            {
                var previous = world.Stars.Concat(world.ArchivedStars).FirstOrDefault(value => value.Id == star.Id);
                return star with
                {
                    Relation = previous?.Relation ?? 0,
                    RelationGainedInPeriod = previous?.RelationGainedInPeriod ?? 0,
                    RelationPeriodStartedAtUtc = previous?.RelationPeriodStartedAtUtc ?? world.Player.PeriodStartedAtUtc,
                };
            }).ToArray(),
            Player = world.Player with
            {
                Name = definition.Player.Name,
                Avatar = definition.Player.Avatar,
                Role = definition.Player.Role,
                Voice = definition.Player.Voice,
                Drama = definition.Player.Drama,
                Wants = definition.Player.Wants.Select(want => want with
                {
                    Done = world.Player.Wants.FirstOrDefault(value => value.Id == want.Id)?.Done ?? false,
                }).ToArray(),
                Achievements = definition.Player.Achievements.Select(item => item with
                {
                    IsOpen = world.Player.Achievements.FirstOrDefault(value => value.Id == item.Id)?.IsOpen ?? false,
                }).ToArray(),
            },
        };
        if (!await store.TryReplaceWorldAsync(updated, world.Version, cancellationToken))
        {
            throw new RequestConflictException("World changed concurrently.");
        }

        return Unit.Value;
    }
}

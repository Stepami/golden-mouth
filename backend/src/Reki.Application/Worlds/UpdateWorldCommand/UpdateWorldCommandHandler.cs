using Mediator;
using Reki.Application.Exceptions;
using Reki.Application.Posts.ML;
using Reki.Application.RekiContext;

namespace Reki.Application.Worlds.UpdateWorldCommand;

internal sealed class UpdateWorldCommandHandler(
    IWorldRepository worldRepository,
    IRekiContextAccessor contextAccessor,
    IStarVectorStore starVectorStore)
    : ICommandHandler<UpdateWorld>
{
    public async ValueTask<Unit> Handle(UpdateWorld command, CancellationToken cancellationToken)
    {
        var world = (await contextAccessor.Context.Worlds).GetValueOrDefault(command.WorldId)
            ?? throw new ResourceNotFoundException("World not found.");
        var definition = command.Definition;
        if (definition.Stars.Count == 0 || definition.Stars.Any(star => !Enum.IsDefined(star.Trait)) ||
            definition.Stars.Select(star => star.Id).Distinct().Count() != definition.Stars.Count)
        {
            throw new ContentRejectedException();
        }

        var updated = definition with
        {
            Id = world.Id,
            Version = world.Version + 1,
            TemplateId = world.TemplateId,
            IsArchived = world.IsArchived,
            Player = world.Player with
            {
                Name = definition.Player.Name,
                Avatar = definition.Player.Avatar,
                Role = definition.Player.Role,
                Voice = definition.Player.Voice,
                Drama = definition.Player.Drama,
                Achievements = definition.Player.Achievements.Select(item => item with
                {
                    IsOpen = world.Player.Achievements.FirstOrDefault(value => value.Id == item.Id)?.IsOpen ?? false,
                }).ToArray(),
            },
        };
        await worldRepository.ReplaceWorldAsync(updated, cancellationToken);
        await starVectorStore.WarmUpAsync(updated);

        return Unit.Value;
    }
}

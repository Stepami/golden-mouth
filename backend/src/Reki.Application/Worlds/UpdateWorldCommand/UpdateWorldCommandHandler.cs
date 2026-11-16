using Mediator;
using Reki.Application.Exceptions;
using Reki.Application.Posts.ML;
using Reki.Application.RekiContext;

namespace Reki.Application.Worlds.UpdateWorldCommand;

internal sealed class UpdateWorldCommandHandler(
    IWorldRepository worldRepository,
    IWorldPlayerCache worldCache,
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

        world.UpdateDefinition(definition);
        await worldRepository.ReplaceWorldAsync(world, cancellationToken);
        worldCache.UpdateWorld(world);
        await starVectorStore.WarmUpAsync(world);

        return Unit.Value;
    }
}

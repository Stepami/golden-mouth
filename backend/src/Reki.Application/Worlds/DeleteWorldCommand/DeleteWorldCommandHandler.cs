using Mediator;
using Reki.Application.Posts;

namespace Reki.Application.Worlds.DeleteWorldCommand;

internal sealed class DeleteWorldCommandHandler(
    IUnitOfWork unitOfWork,
    IWorldRepository worlds,
    IWorldPlayerCache worldCache,
    IPostRepository posts) : ICommandHandler<DeleteWorld>
{
    public async ValueTask<Unit> Handle(DeleteWorld command, CancellationToken cancellationToken)
    {
        await unitOfWork.ExecuteAsync(
            async token =>
            {
                await posts.DeletePostsAsync(command.PlayerId, [command.WorldId], token);
                await worlds.DeleteWorldsAsync(command.PlayerId, [command.WorldId], token);
                return Unit.Value;
            },
            cancellationToken);
        worldCache.ClearPlayer(command.PlayerId);
        return Unit.Value;
    }
}

using Mediator;
using Reki.Application.Posts;

namespace Reki.Application.Worlds.DeleteWorldCommand;

internal sealed class DeleteWorldCommandHandler(
    IUnitOfWork unitOfWork,
    IWorldRepository worlds,
    IPostRepository posts) : ICommandHandler<DeleteWorld>
{
    public async ValueTask<Unit> Handle(DeleteWorld command, CancellationToken cancellationToken) =>
        await unitOfWork.ExecuteAsync(
            async token =>
            {
                await posts.DeletePostsAsync([command.WorldId], token);
                await worlds.DeleteWorldsAsync([command.WorldId], token);
                return Unit.Value;
            },
            cancellationToken);
}

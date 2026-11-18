using Mediator;
using Reki.Application.Player;
using Reki.Application.Posts;
using Reki.Application.RekiContext;

namespace Reki.Application.Worlds.DeleteWorldCommand;

internal sealed class DeleteWorldCommandHandler(
    IUnitOfWork unitOfWork,
    IWorldRepository worlds,
    IWorldPlayerCache worldCache,
    IPlayerRepository players,
    IRekiContextAccessor contextAccessor,
    IPostRepository posts) : ICommandHandler<DeleteWorld>
{
    public async ValueTask<Unit> Handle(DeleteWorld command, CancellationToken cancellationToken)
    {
        var player = await contextAccessor.Context.Player;
        return await unitOfWork.ExecuteAsync(
            async token =>
            {
                await posts.DeletePostsAsync(command.PlayerId, [command.WorldId], token);
                await worlds.DeleteWorldsAsync(command.PlayerId, [command.WorldId], token);
                var previousVersion = player.Version;
                player.RemoveDailyChallengeWorld(command.WorldId);
                if (player.Version != previousVersion)
                {
                    await players.SaveDailyChallengeAsync(player, previousVersion, token);
                }

                return Unit.Value;
            },
            _ => worldCache.ClearWorld(command.PlayerId, command.WorldId),
            cancellationToken);
    }
}

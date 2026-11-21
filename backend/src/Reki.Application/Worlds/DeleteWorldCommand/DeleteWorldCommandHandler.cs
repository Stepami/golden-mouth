using Mediator;
using Reki.Application.Exceptions;
using Reki.Application.Player;
using Reki.Application.Posts;

namespace Reki.Application.Worlds.DeleteWorldCommand;

internal sealed class DeleteWorldCommandHandler(
    IUnitOfWork unitOfWork,
    IWorldRepository worlds,
    IWorldPlayerCache worldCache,
    IPlayerRepository players,
    IPostRepository posts) : ICommandHandler<DeleteWorld>
{
    public async ValueTask<Unit> Handle(DeleteWorld command, CancellationToken cancellationToken)
    {
        return await unitOfWork.ExecuteAsync(
            async token =>
            {
                var player = await players.GetPlayerAsync(command.PlayerId, token)
                             ?? throw new ResourceNotFoundException("Player not found.");
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

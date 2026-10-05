using Mediator;
using Reki.Application.Common.Exceptions;
using Reki.Application.Game.Common;
using Reki.Domain.Game.Player;
using Reki.Domain.Game.Posts;

namespace Reki.Application.Game.Posts;

public sealed class AddReplyHandler(IGameStore store, PlayerReader playerReader)
    : IRequestHandler<AddReplyCommand>
{
    public async ValueTask<Unit> Handle(
        AddReplyCommand request,
        CancellationToken cancellationToken)
    {
        PlayerProfile player = await playerReader.GetAsync(request.TelegramUserId, cancellationToken);
        string replyId = Guid.NewGuid().ToString("N");
        var reply = new GameComment(
            replyId,
            player.Name,
            player.Avatar,
            player.Handle,
            request.Text,
            false,
            true,
            false);
        bool updated = await store.AddReplyAsync(
            player.Id,
            request.PostId,
            reply,
            cancellationToken);
        if (!updated)
        {
            throw new ResourceNotFoundException("Post not found.");
        }

        return Unit.Value;
    }
}


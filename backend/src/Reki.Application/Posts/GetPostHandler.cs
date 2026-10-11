using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Contracts.Posts;
using Reki.Domain.Player;
using TgMiniAppAuth.AuthContext.User;

namespace Reki.Application.Posts;

public sealed class GetPostHandler(
    ITelegramUserAccessor telegramUser,
    IGameStore store, PlayerReader playerReader)
    : IRequestHandler<GetPostQuery, PostDetailResponse>
{
    public async ValueTask<PostDetailResponse> Handle(
        GetPostQuery request,
        CancellationToken cancellationToken)
    {
        var telegramUserId = TelegramUserId.From(telegramUser.User.Id);
        var player = await playerReader.GetAsync(telegramUserId, cancellationToken);
        var post = await store.FindPostAsync(request.PostId, cancellationToken)
                   ?? throw new ResourceNotFoundException("Post not found.");
        var canReply = post.PlayerId == player.Id;
        if (!canReply)
        {
            var author = await store.GetPlayerAsync(post.PlayerId, cancellationToken)
                         ?? throw new ResourceNotFoundException("Post not found.");
            if (author.World != player.World)
            {
                throw new ResourceNotFoundException("Post not found.");
            }
        }

        return PostContractMapper.MapPostDetail(post, canReply);
    }
}

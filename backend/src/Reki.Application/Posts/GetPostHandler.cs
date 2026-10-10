using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Contracts.Posts;
using Reki.Domain.Player;
using Reki.Domain.Posts;
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
        TelegramUserId telegramUserId = TelegramUserId.From(telegramUser.User.Id);
        PlayerProfile player = await playerReader.GetAsync(telegramUserId, cancellationToken);
        GamePost post = await store.FindPostAsync(request.PostId, cancellationToken)
            ?? throw new ResourceNotFoundException("Post not found.");
        bool canReply = post.PlayerId == player.Id;
        if (!canReply)
        {
            PlayerProfile author = await store.GetPlayerAsync(post.PlayerId, cancellationToken)
                ?? throw new ResourceNotFoundException("Post not found.");
            if (author.World != player.World)
            {
                throw new ResourceNotFoundException("Post not found.");
            }
        }

        return PostContractMapper.MapPostDetail(post, canReply);
    }
}

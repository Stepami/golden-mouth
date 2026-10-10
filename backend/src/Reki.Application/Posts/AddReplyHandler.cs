using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Contracts.Posts;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using TgMiniAppAuth.AuthContext.User;

namespace Reki.Application.Posts;

public sealed class AddReplyHandler(
    ITelegramUserAccessor telegramUser,
    IGameStore store,
    GameBalance balance,
    PlayerReader playerReader)
    : IRequestHandler<AddReplyCommand, ReplyResponse>
{
    private const int MaxAttempts = 3;

    public async ValueTask<ReplyResponse> Handle(
        AddReplyCommand request,
        CancellationToken cancellationToken)
    {
        TelegramUserId telegramUserId = TelegramUserId.From(telegramUser.User.Id);
        for (int attempt = 0; attempt < MaxAttempts; attempt++)
        {
            PlayerProfile player = await playerReader.GetAsync(
                telegramUserId,
                cancellationToken);
            GamePost post = await store.GetPostAsync(
                player.Id,
                request.PostId,
                cancellationToken) ?? throw new ResourceNotFoundException("Post not found.");

            ThreadReply? existing = post.FindReply(request.ReplyId);
            if (existing is not null)
            {
                return Map(existing);
            }

            bool targetExists = post.Comments.Any(comment =>
                comment.Id == request.TargetCommentId && !comment.Me);
            if (!targetExists)
            {
                throw new ResourceNotFoundException("Comment not found.");
            }

            ThreadReply reply = ThreadReplyPolicy.CreateReply(
                post,
                request.ReplyId,
                CommentId.New(),
                request.TargetCommentId,
                player.Subscribers,
                balance.Thread.BonusReplies,
                balance.Thread.SubscriberBonus,
                balance.Thread.BranchDepth);
            var comment = new GameComment(
                reply.CommentId,
                player.Name,
                player.Avatar,
                player.Handle,
                request.Text,
                false,
                true,
                false);
            PlayerProfile? updated = reply.BonusAwarded > 0
                ? player.AwardThreadBonus(reply.BonusAwarded)
                : null;
            bool added = await store.TryAddThreadReplyAsync(
                player.Id,
                request.PostId,
                reply,
                comment,
                updated,
                player.Version,
                cancellationToken);
            if (added)
            {
                return Map(reply);
            }
        }

        throw new RequestConflictException("The thread reply could not be saved.");
    }

    private ReplyResponse Map(ThreadReply reply) =>
        new(
            reply.ReplyId,
            reply.CommentId.Value,
            reply.BonusAwarded,
            reply.AwardedReplies,
            balance.Thread.BonusReplies,
            reply.SubscriberTotal,
            reply.Reaction);
}


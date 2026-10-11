using Mediator;
using Microsoft.Extensions.Options;
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
    IOptions<GameBalance> balance,
    PlayerReader playerReader)
    : IRequestHandler<AddReplyCommand, ReplyResponse>
{
    public async ValueTask<ReplyResponse> Handle(
        AddReplyCommand request,
        CancellationToken cancellationToken)
    {
        var telegramUserId = TelegramUserId.From(telegramUser.User.Id);
        var player = await playerReader.GetAsync(
            telegramUserId,
            cancellationToken);
        var post = await store.GetPostAsync(
            request.PostId,
            player.Id,
            cancellationToken) ?? throw new ResourceNotFoundException("Post not found.");

        var existing = post.FindReply(request.ReplyId);
        if (existing is not null)
        {
            return Map(existing);
        }

        var targetExists = post.Comments.Any(comment =>
            comment.Id == request.TargetCommentId && !comment.Me);
        if (!targetExists)
        {
            throw new ResourceNotFoundException("Comment not found.");
        }

        var reply = ThreadReplyPolicy.CreateReply(
            post,
            request.ReplyId,
            CommentId.New(),
            request.TargetCommentId,
            player.Subscribers,
            balance.Value.Thread.BonusReplies,
            balance.Value.Thread.SubscriberBonus,
            balance.Value.Thread.BranchDepth);
        var comment = new GameComment(
            reply.CommentId,
            player.Name,
            player.Avatar,
            player.Handle,
            request.Text,
            false,
            true,
            false);
        var updated = reply.BonusAwarded > 0
            ? player.AwardThreadBonus(reply.BonusAwarded)
            : null;
        var added = await store.TryAddThreadReplyAsync(
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

        throw new RequestConflictException("The thread reply could not be saved.");
    }

    private ReplyResponse Map(ThreadReply reply) =>
        new(
            reply.ReplyId,
            reply.CommentId.Value,
            reply.BonusAwarded,
            reply.AwardedReplies,
            balance.Value.Thread.BonusReplies,
            reply.SubscriberTotal,
            reply.Reaction);
}

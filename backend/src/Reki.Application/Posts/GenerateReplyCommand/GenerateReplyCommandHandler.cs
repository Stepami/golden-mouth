using System.Runtime.CompilerServices;
using Mediator;
using Reki.Application.Exceptions;
using Reki.Application.Posts.AI;
using Reki.Application.Posts.AI.Safety;
using Reki.Application.Posts.GetPost;
using Reki.Application.Posts.Responses.Reply.Generation;
using Reki.Application.RekiContext;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.GenerateReplyCommand;

internal sealed class GenerateReplyCommandHandler(
    ISender sender,
    IRekiContextAccessor rekiContextAccessor,
    StarTurnPolicy turnPolicy,
    GeneratePostReply postGeneration,
    ReplyTextGenerator replyText) : IStreamCommandHandler<GenerateReply, ReplyGenerationLiveEvent>
{
    private const double AdditionalStarProbability = 0.5;

    public async IAsyncEnumerable<ReplyGenerationLiveEvent> Handle(
        GenerateReply request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var rekiContext = rekiContextAccessor.Context;
        var player = await rekiContext.Player;
        var world = (await rekiContext.Worlds).GetValueOrDefault(request.WorldId)
            ?? throw new ResourceNotFoundException("World not found.");
        var post = await sender.Send(new PostQuery(request.PostId, world.Id), cancellationToken);
        if (request.SourceCommentId is null)
        {
            if (request.AuthorId is not null || request.Additional || post.AuthorId != player.Id.ToAuthorId())
            {
                throw new RequestConflictException("Invalid post generation request.");
            }

            await foreach (var item in postGeneration.GenerateAsync(post, player, world, cancellationToken))
            {
                yield return item;
            }

            yield break;
        }

        if (request.AuthorId is null)
        {
            throw new RequestConflictException("The thread author is required.");
        }

        var star = world.FindStar(request.AuthorId.Value)
            ?? throw new RequestConflictException("The author is not a star of this world.");
        var source = post.Comments.FirstOrDefault(item =>
                item.Id == request.SourceCommentId && !item.Hidden && item.AuthorId == player.Id.ToAuthorId())
            ?? throw new ResourceNotFoundException("Player comment not found.");
        var primary = source.ParentCommentId is { } parentId
            ? world.FindStar(post.Comments.First(item => item.Id == parentId).AuthorId)
            : world.FindStar(post.AuthorId);
        if (primary is null)
        {
            throw new RequestConflictException("The thread star is no longer in this world.");
        }

        var others = world.Stars.Where(item => item.Id != primary.Id).ToArray();
        var bytes = source.Id.Value.ToByteArray();
        StarId? additionalStarId = others.Length > 0 &&
                                   BitConverter.ToUInt32(bytes, 0) / 4294967296d < AdditionalStarProbability
            ? others[(int)(BitConverter.ToUInt32(bytes, 4) % (uint)others.Length)].Id
            : null;
        if (request.Additional ? additionalStarId != star.Id : primary.Id != star.Id)
        {
            throw new RequestConflictException("The star does not belong to the selected thread.");
        }

        var branch = source.ParentCommentId is { } parent ? post.GetBranch(parent) : [];
        var turnContext = new StarReplyContext(
            new StarAgentProfile(world, star.Id, player.Address),
            star.Id,
            post.Text,
            branch,
            source.Text,
            turnPolicy.IsHateEnabled(player, world),
            request.Additional);
        await foreach (var item in replyText.GenerateAsync(turnContext, request.AuthorId.Value, request.Additional ? null : additionalStarId, cancellationToken))
        {
            yield return item;
        }
    }
}

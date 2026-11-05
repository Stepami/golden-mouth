using Mediator;
using Microsoft.Extensions.Options;
using Reki.Application.Balance;
using Reki.Application.Exceptions;
using Reki.Application.Posts.AI.Safety;
using Reki.Application.Posts.Responses;
using Reki.Application.RekiContext;
using Reki.Domain.Posts;

namespace Reki.Application.Posts.CreatePostCommand;

internal sealed class CreatePostCommandHandler(IGameStore store, IPostRepository posts, IOptionsMonitor<GameBalance> balance, TimeProvider timeProvider, IRekiContextAccessor accessor, PlayerSafetyGuard safety) : ICommandHandler<CreatePost, CreatePostResponse>
{
    public async ValueTask<CreatePostResponse> Handle(CreatePost request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.RequestId, out var id))
        {
            throw new RequestConflictException("The request identifier is invalid.");
        }

        var context = accessor.Context;
        var player = await context.Player;
        var world = (await context.Worlds).GetValueOrDefault(request.WorldId) ?? throw new ResourceNotFoundException("World not found.");
        var postId = PostId.New(timeProvider.GetUtcNow());
        var existing = await posts.GetByRequestAsync(world.Id, player.Id.ToAuthorId(), id.ToString("N"), cancellationToken);
        if (existing is not null)
        {
            return Replay(existing);
        }

        await safety.EnsureInputAllowedAsync(player, [request.Text], cancellationToken);
        if (player.Energy < balance.CurrentValue.PostEnergyCost)
        {
            throw new RequestConflictException("Insufficient energy.");
        }

        if (request.WantId is not null && !world.Player.Wants.Any(want => want.Id == request.WantId && !want.Done))
        {
            throw new RequestConflictException("The selected daily want is unavailable.");
        }

        var post = new GamePost(postId, player.Id.ToAuthorId(), world.Id, request.Text, Math.Max(48, world.Player.Subscribers), [], timeProvider.GetUtcNow(), 0, 0, 1, false, false, 0) { EvaluationApplied = false, RequestId = id.ToString("N"), OwnWords = request.OwnWords, EnergyIndex = player.EnergyUsed + 1, WantId = request.WantId };
        if (!await store.TryCreatePostAsync(post, player.SpendEnergy(balance.CurrentValue.PostEnergyCost), player.Version, cancellationToken))
        {
            existing = await posts.GetByRequestAsync(world.Id, player.Id.ToAuthorId(), id.ToString("N"), cancellationToken);
            if (existing is null)
            {
                throw new RequestConflictException("The player state changed.");
            }

            return Replay(existing);
        }

        return new CreatePostResponse(postId);

        CreatePostResponse Replay(GamePost saved)
        {
            if (saved.AuthorId != player.Id.ToAuthorId() || saved.WorldId != world.Id || saved.Text != request.Text || saved.OwnWords != request.OwnWords || saved.WantId != request.WantId)
            {
                throw new RequestConflictException("The request identifier belongs to another post.");
            }

            return new CreatePostResponse(saved.Id);
        }
    }
}

using Mediator;
using Microsoft.Extensions.Options;
using Reki.Application.Balance;
using Reki.Application.Exceptions;
using Reki.Application.Player;
using Reki.Application.Posts.Responses;
using Reki.Application.RekiContext;
using Reki.Application.Telemetry;
using Reki.Application.Worlds;
using Reki.Domain.Posts;

namespace Reki.Application.Posts.RepostCommand;

internal sealed class RepostCommandHandler(
    IUnitOfWork unitOfWork,
    IPlayerRepository players,
    IWorldRepository worlds,
    IPostRepository posts,
    IRekiContextAccessor contextAccessor,
    IOptionsMonitor<GameBalance> balance,
    TimeProvider timeProvider,
    ProductTelemetry telemetry) : ICommandHandler<Repost, RepostResponse>
{
    public async ValueTask<RepostResponse> Handle(Repost command, CancellationToken cancellationToken)
    {
        var playerId = contextAccessor.Context.PlayerId;
        var response = await unitOfWork.ExecuteAsync(
            async token =>
        {
            // Read again on every transaction retry: concurrent taps must not spend or award twice.
            var player = await players.GetPlayerAsync(playerId, token)
                ?? throw new ResourceNotFoundException("Player not found.");
            var world = await worlds.GetWorldAsync(playerId, command.WorldId, token)
                ?? throw new ResourceNotFoundException("World not found.");
            var selected = await posts.GetPostAsync(command.PostId, world.Id, token)
                ?? throw new ResourceNotFoundException("Post not found.");
            var source = selected;
            GamePost? repost = null;
            if (selected.IsRepost && !command.Active && selected.AuthorId == playerId.ToAuthorId())
            {
                repost = selected;
                source = selected.QuotedPostId is { } originalId
                    ? await posts.GetPostAsync(originalId, world.Id, token)
                    : null;
            }
            else
            {
                if (world.FindStar(source.AuthorId) is null || source.QuotedPostId is not null)
                {
                    throw new RequestConflictException("Only an original star post can be reposted.");
                }

                repost = source.RepostId is { } repostId
                    ? await posts.GetPostAsync(repostId, world.Id, token)
                    : null;
            }

            long delta = 0;
            if (command.Active && repost is null)
            {
                if (source is null || source.RepostId is not null || world.IsArchived || player.PausedAtUtc is not null)
                {
                    throw new RequestConflictException("The repost is unavailable.");
                }

                var currentBalance = balance.CurrentValue;
                if (player.Wallet.Energy < currentBalance.PostEnergyCost)
                {
                    throw new RequestConflictException("Insufficient energy.");
                }

                var postIndex = player.Wallet.EnergyUsed + 1;
                var decay = currentBalance.Growth.Decay.First(item => postIndex >= item.From && postIndex <= item.To).Multiplier;
                var growth = currentBalance.Growth.Coefficient *
                    currentBalance.Difficulties[world.Difficulty].GrowthCoefficient * 0.5 *
                    Math.Pow(Math.Max(1, world.Player.Subscribers), currentBalance.Growth.Power) *
                    decay * currentBalance.RepostGrowthMultiplier;
                delta = Math.Max(1, (long)Math.Round(growth));
                var now = timeProvider.GetUtcNow();
                repost = new GamePost(PostId.New(now), playerId.ToAuthorId(), world.Id, string.Empty, 0, [], now, delta, 0, 1, false, false, QuotedPostId: source.Id, IsRepost: true);
                player = player.SpendEnergy(currentBalance.PostEnergyCost, now) with
                {
                    HitOwnWorldMilestone = player.HitOwnWorldMilestone || world.Player.Subscribers + delta >= currentBalance.OwnWorldSubscribers,
                };
                world = world with
                {
                    Player = world.Player with { Subscribers = world.Player.Subscribers + delta },
                    Version = world.Version + 1,
                };
                await players.ReplacePlayerAsync(player, token);
                await worlds.ReplaceWorldAsync(world, token);
                await posts.InsertPostAsync(repost, token);
                source = source with { RepostId = repost.Id };
            }

            if (repost is not null)
            {
                await posts.SaveRepostStateAsync(repost with { RepostHidden = !command.Active }, token);
            }

            if (source is not null)
            {
                await posts.SaveRepostStateAsync(source with { RepostActive = command.Active && repost is not null }, token);
            }

            return new RepostResponse(repost?.Id, command.Active && repost is not null, delta, player.Wallet.Energy, world.Player.Subscribers);
        },
            cancellationToken);
        var currentWorld = await worlds.GetWorldAsync(playerId, command.WorldId, cancellationToken);
        if (response.SubscriberDelta > 0 && currentWorld is not null)
        {
            telemetry.TrackWorldAudience(currentWorld);
        }

        telemetry.TrackPlayer(
            await contextAccessor.Context.Player,
            command.Active ? "post_repost" : "post_repost_undo",
            new Dictionary<string, string>
            {
                ["publication_kind"] = "repost",
                ["rewarded"] = response.SubscriberDelta > 0 ? "true" : "false",
            },
            command.WorldId);
        return response;
    }
}

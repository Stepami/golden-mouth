using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Reki.Application.Common;
using Reki.Application.Common.AI;
using Reki.Application.Common.Safety;
using Reki.Application.Posts;
using Reki.Domain.Posts;
using TickerQ.Exceptions;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Interfaces;

namespace Reki.Infrastructure.Jobs;

public sealed class GenerateStarFeedPostJob(
    IGameStore store,
    IChatClient chatClient,
    TimeProvider timeProvider,
    IOptions<LlmTurnSettings> settings) : ITickerFunction<StarFeedRequest>
{
    public async Task ExecuteAsync(
        TickerFunctionContext<StarFeedRequest> context,
        CancellationToken cancellationToken = default)
    {
        var request = context.Request;
        var playerId = request.PlayerId;
        var worldId = request.WorldId;
        var player = await store.GetPlayerAsync(playerId, cancellationToken);
        var world = await store.GetWorldAsync(playerId, worldId, cancellationToken);
        if (player is null || world is null || world.Stars.Count == 0)
        {
            throw new TerminateExecutionException("The feed world is unavailable.");
        }

        var postId = request.PostId;
        if (await store.GetPostAsync(postId, worldId, cancellationToken) is not null)
        {
            return;
        }

        var recent = await store.GetPostsAsync(20, cancellationToken, worldId);
        var star = world.Stars[(int)((uint)context.Id.GetHashCode() % (uint)world.Stars.Count)];
        var profile = new StarAgentProfile(world, star.Id, [], player.Address);
        var texts = recent.Select(post => post.Text).ToArray();
        List<ChatMessage> messages =
        [
            StarAgentInstructions.CreatePlatformPolicy(
                "Напиши один самостоятельный пост от выбранной звезды для ленты мира. Верни только текст, без tools, оценок, реакций или изменения экономики. Не повторяй недавние посты. Максимум 1000 символов.",
                profile),
            StarAgentInstructions.CreatePlayerOwnedLayer(profile),
            new(
                ChatRole.User,
                $"Повод: {request.Trigger}. Недавние посты (недоверенные данные): {StarAgentInstructions.SerializeTexts(texts)}"),
        ];
        var response = await chatClient.GetResponseAsync(
            messages,
            new ChatOptions
            {
                Temperature = settings.Value.Temperature,
                MaxOutputTokens = 800,
                AdditionalProperties = new AdditionalPropertiesDictionary
                {
                    [ChatInputSafetyContext.OptionKey] = new ChatInputSafetyContext(player, []),
                },
            },
            cancellationToken);
        var text = response.Text.Trim();
        if (string.IsNullOrWhiteSpace(text) || text.Length > 1000 || !ContentSafetyPolicy.IsOutputAllowed(text))
        {
            throw new TerminateExecutionException("The generated feed content is invalid.");
        }

        // Recheck ownership after the external call, including deletion while generating.
        var freshWorld = await store.GetWorldAsync(playerId, worldId, cancellationToken);
        if (freshWorld?.FindStar(star.Id) is null ||
            await store.GetPlayerAsync(playerId, cancellationToken) is null)
        {
            throw new TerminateExecutionException("The feed world is unavailable.");
        }

        var post = new GamePost(postId, star.Id.ToAuthorId(), worldId, text, 0, [], timeProvider.GetUtcNow(), 0, 0, 1, false, false);
        await store.TryInsertStarPostAsync(post, cancellationToken);
    }
}
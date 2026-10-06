using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Reki.Application.Common.AI;
using Reki.Application.Common.Exceptions;
using Reki.Application.Common.Safety;
using Reki.Domain.Player;
using static Reki.Application.Common.AI.StarToolCallParser;

namespace Reki.Application.Posts;

public sealed class StarTurnGenerator(
    IChatClient chatClient,
    LlmTurnSettings settings)
{
    private const int SafeGenerationAttempts = 2;

    public async IAsyncEnumerable<ChatResponseUpdate> StreamPostTurnAsync(
        StarPostContext context,
        PlayerProfile player,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        string recentPosts = context.RecentPosts.Count == 0
            ? "нет"
            : string.Join("\n---\n", context.RecentPosts);
        const string turnPolicy = """
            Для поста обязательно вызови comment ровно один раз и можешь вызвать react
            не более одного раза. Оцени текст по трём шкалам мира от 0 до 1.
            """;
        string userPrompt = $$"""
            Этап: {{context.Stage}}
            Хейт разрешён сейчас: {{context.HateEnabled}}
            Последние посты:
            {{recentPosts}}

            Новый пост:
            {{context.Text}}
            """;
        List<ChatMessage> messages =
        [
            StarAgentInstructions.CreatePlatformPolicy(turnPolicy),
            StarAgentInstructions.CreatePlayerOwnedLayer(context.Agent),
            new(ChatRole.User, userPrompt),
        ];
        ChatOptions options = CreateOptions(
            ChatToolMode.RequireSpecific("comment"),
            new ChatInputSafetyContext(player, [context.Text]));
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(settings.Timeout);
        await foreach (ChatResponseUpdate update in chatClient
            .GetStreamingResponseAsync(messages, options, timeoutSource.Token)
            .WithCancellation(timeoutSource.Token))
        {
            yield return update;
        }
    }

    public async ValueTask<GeneratedPostTurn> GeneratePostTurnAsync(
        StarPostContext context,
        PlayerProfile player,
        CancellationToken cancellationToken)
    {
        string recentPosts = context.RecentPosts.Count == 0
            ? "нет"
            : string.Join("\n---\n", context.RecentPosts);
        const string turnPolicy = """
            Для поста обязательно вызови comment ровно один раз и можешь вызвать react
            не более одного раза. Оцени текст по трём шкалам мира от 0 до 1.
            """;
        string userPrompt = $$"""
            Этап: {{context.Stage}}
            Хейт разрешён сейчас: {{context.HateEnabled}}
            Последние посты:
            {{recentPosts}}

            Новый пост:
            {{context.Text}}
            """;
        List<ChatMessage> messages =
        [
            StarAgentInstructions.CreatePlatformPolicy(turnPolicy),
            StarAgentInstructions.CreatePlayerOwnedLayer(context.Agent),
            new(ChatRole.User, userPrompt),
        ];

        IReadOnlyList<IStarAction> actions = await GetActionsAsync(
            messages,
            ChatToolMode.RequireSpecific("comment"),
            new ChatInputSafetyContext(player, [context.Text]),
            cancellationToken);
        StarCommentAction[] comments = actions.OfType<StarCommentAction>().ToArray();
        StarReactionAction[] reactions = actions.OfType<StarReactionAction>().ToArray();
        if (comments.Length != 1 || reactions.Length > 1)
        {
            throw new AiServiceUnavailableException("The model returned an invalid action set.");
        }

        StarCommentAction comment = comments[0];
        if (!ContentSafetyPolicy.IsOutputAllowed(comment.Text) ||
            !ContentSafetyPolicy.IsOutputAllowed(comment.Hook))
        {
            throw new AiServiceUnavailableException("The model output did not pass the safety policy.");
        }

        return new GeneratedPostTurn(
            comment.Author,
            comment.StarId,
            comment.Text,
            comment.Hook,
            reactions.FirstOrDefault()?.Emoji ?? "🔥",
            comment.Brave,
            comment.Humor,
            comment.Topic,
            comment.Reasons,
            comment.OnTopic);
    }

    public async ValueTask<GeneratedDirectMessage> GenerateDirectMessageAsync(
        StarDirectMessageContext context,
        PlayerProfile player,
        CancellationToken cancellationToken)
    {
        const string turnPolicy = """
            Ответь только через tool dm. memory_fact — один короткий безопасный факт
            об интересах игрока или пустая строка.
            """;
        string userPrompt = $$"""
            Сообщение игрока: {{context.Text}}
            """;
        List<ChatMessage> messages =
        [
            StarAgentInstructions.CreatePlatformPolicy(turnPolicy),
            StarAgentInstructions.CreatePlayerOwnedLayer(context.Agent),
            new(ChatRole.User, userPrompt),
        ];

        for (int attempt = 0; attempt < SafeGenerationAttempts; attempt++)
        {
            IReadOnlyList<IStarAction> actions = await GetActionsAsync(
                messages,
                ChatToolMode.RequireSpecific("dm"),
                new ChatInputSafetyContext(player, [context.Text]),
                cancellationToken);
            StarDirectMessageAction[] directMessages = actions
                .OfType<StarDirectMessageAction>()
                .ToArray();
            if (directMessages is [var directMessage] &&
                directMessage.StarId == context.Agent.StarId &&
                ContentSafetyPolicy.IsOutputAllowed(directMessage.Text) &&
                ContentSafetyPolicy.IsOutputAllowed(directMessage.MemoryFact))
            {
                return new GeneratedDirectMessage(directMessage.Text, directMessage.MemoryFact);
            }

            messages.Add(new ChatMessage(
                ChatRole.System,
                "Предыдущий вариант был невалидным или не прошёл фильтр 14+. Повтори безопасно."));
        }

        throw new AiServiceUnavailableException("The model returned an invalid direct message.");
    }

    private async ValueTask<IReadOnlyList<IStarAction>> GetActionsAsync(
        IReadOnlyList<ChatMessage> messages,
        ChatToolMode toolMode,
        ChatInputSafetyContext safetyContext,
        CancellationToken cancellationToken)
    {
        ChatOptions options = CreateOptions(toolMode, safetyContext);
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(settings.Timeout);

        try
        {
            ChatResponse response = await chatClient.GetResponseAsync(
                messages,
                options,
                timeoutSource.Token);
            IEnumerable<FunctionCallContent> calls = response.Messages
                .SelectMany(message => message.Contents)
                .OfType<FunctionCallContent>();
            return StarToolCallParser.Parse(calls);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AiServiceUnavailableException("The model request timed out.", exception);
        }
        catch (AiServiceUnavailableException)
        {
            throw;
        }
        catch (ContentRejectedException)
        {
            throw;
        }
        catch (CrisisDetectedException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new AiServiceUnavailableException("The model request failed.", exception);
        }
    }

    private ChatOptions CreateOptions(
        ChatToolMode toolMode,
        ChatInputSafetyContext safetyContext)
    {
        return new ChatOptions
        {
            ModelId = settings.Model,
            Temperature = settings.Temperature,
            MaxOutputTokens = settings.MaxOutputTokens,
            Tools = [.. StarToolSchemas.All],
            ToolMode = toolMode,
            AllowMultipleToolCalls = true,
            AdditionalProperties = new AdditionalPropertiesDictionary
            {
                [ChatInputSafetyContext.OptionKey] = safetyContext,
            },
        };
    }
}

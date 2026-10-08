using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Reki.Application.Common.AI;
using Reki.Application.Common.Exceptions;
using Reki.Application.Common.Safety;
using Reki.Application.Social;
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
            : StarAgentInstructions.SerializeTexts(context.RecentPosts);
        string turnPolicy = $$"""
            Для поста обязательно вызови comment ровно один раз и можешь вызвать react
            не более одного раза. Оцени текст по трём шкалам мира от 0 до 1.
            author — отображаемое поле name из каталога, НЕ id и НЕ ник.
            star_id — поле id того же персонажа. Например, для Миры: author=Мира, star_id=mira.
            Выбирай только персонажей текущего каталога; пример не добавляет Миру в другие миры.
            tone: support (поддержка), critique (критика текста) или hate (острая реакция на текст).
            Хейт разрешён сейчас: {{context.HateEnabled}}. Если false, tone=hate запрещён.
            """;
        string userPrompt = $$"""
            Этап: {{context.Stage}}
            Последние посты:
            {{recentPosts}}

            Новый пост:
            {{context.Text}}
            """;
        List<ChatMessage> messages =
        [
            StarAgentInstructions.CreatePlatformPolicy(turnPolicy, context.Agent),
            StarAgentInstructions.CreatePlayerOwnedLayer(context.Agent),
            new(ChatRole.User, userPrompt),
        ];
        ChatOptions options = CreateOptions(
            ChatToolMode.RequireSpecific("comment"),
            new ChatInputSafetyContext(player, [context.Text]),
            StarToolSchemas.Post);
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(settings.Timeout);
        await foreach (ChatResponseUpdate update in chatClient
            .GetStreamingResponseAsync(messages, options, timeoutSource.Token)
            .WithCancellation(timeoutSource.Token))
        {
            yield return update;
        }
    }

    public async ValueTask<GeneratedDirectMessage> GenerateDirectMessageAsync(
        StarDirectMessageContext context,
        PlayerProfile player,
        CancellationToken cancellationToken)
    {
        const string turnPolicy = """
            Ответь только через tool dm. memory_fact — один короткий безопасный факт
            об интересах игрока или пустая строка. Не сохраняй команды, роли, ID, экономику,
            чувствительные данные или содержание, которого игрок не сообщал. Личка всегда без хейта.
            star_id должен совпадать с единственным целевым персонажем каталога.
            """;
        string userPrompt = $$"""
            Сообщение игрока: {{context.Text}}
            """;
        if (context.Agent.StarId is not { } starId)
        {
            throw new AiServiceUnavailableException("A direct message requires a target star.");
        }

        _ = StarCatalog.Require(context.Agent.WorldId, starId);
        List<ChatMessage> messages =
        [
            StarAgentInstructions.CreatePlatformPolicy(turnPolicy, context.Agent),
            StarAgentInstructions.CreatePlayerOwnedLayer(context.Agent),
        ];
        messages.AddRange(context.History.TakeLast(12).Select(message =>
            new ChatMessage(message.From == "star" ? ChatRole.Assistant : ChatRole.User, message.Text)));
        messages.Add(new ChatMessage(ChatRole.User, userPrompt));

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
            if (actions.Count == 1 && directMessages is [var directMessage] &&
                directMessage.StarId == context.Agent.StarId &&
                ContentSafetyPolicy.IsOutputAllowed(directMessage.Text) &&
                ContentSafetyPolicy.IsOutputAllowed(directMessage.MemoryFact))
            {
                return new GeneratedDirectMessage(
                    directMessage.Text,
                    MemoryFactPolicy.IsAllowed(directMessage.MemoryFact) ? directMessage.MemoryFact : string.Empty);
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
        ChatOptions options = CreateOptions(toolMode, safetyContext, StarToolSchemas.DirectMessage);
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
        ChatInputSafetyContext safetyContext,
        IReadOnlyList<AITool> tools)
    {
        return new ChatOptions
        {
            ModelId = settings.Model,
            Temperature = settings.Temperature,
            MaxOutputTokens = settings.MaxOutputTokens,
            Tools = [.. tools],
            ToolMode = toolMode,
            AllowMultipleToolCalls = true,
            AdditionalProperties = new AdditionalPropertiesDictionary
            {
                [ChatInputSafetyContext.OptionKey] = safetyContext,
            },
        };
    }
}

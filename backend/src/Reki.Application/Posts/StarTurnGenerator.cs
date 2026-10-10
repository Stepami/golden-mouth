using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Reki.Application.Common.AI;
using Reki.Application.Common.Exceptions;
using Reki.Application.Common.Safety;
using Reki.Application.Social;
using Reki.Domain.Player;
using static Reki.Application.Common.AI.StarToolCallParser;

namespace Reki.Application.Posts;

public sealed class StarTurnGenerator(
    IChatClient chatClient,
    IOptions<LlmTurnSettings> settings)
{
    public async IAsyncEnumerable<ChatResponseUpdate> StreamPostTurnAsync(
        StarPostContext context,
        PlayerProfile player,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var recentPosts = context.RecentPosts.Count == 0
            ? "нет"
            : StarAgentInstructions.SerializeTexts(context.RecentPosts);
        var turnPolicy = $$"""
                           Для поста обязательно вызови comment ровно один раз и можешь вызвать react
                           не более одного раза. Оцени текст по трём шкалам мира от 0 до 1.
                           author — отображаемое поле name из каталога, НЕ id и НЕ ник.
                           star_id — поле id того же персонажа. Например, для Миры: author=Мира, star_id=mira.
                           Выбирай только персонажей текущего каталога; пример не добавляет Миру в другие миры.
                           tone: support (поддержка), critique (критика текста) или hate (острая реакция на текст).
                           Хейт разрешён сейчас: {{context.HateEnabled}}. Если false, tone=hate запрещён.
                           """;
        var userPrompt = $$"""
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
        var options = CreateOptions(
            ChatToolMode.RequireSpecific("comment"),
            new ChatInputSafetyContext(player, [context.Text]),
            StarToolSchemas.Post);
        await foreach (var update in chatClient
                           .GetStreamingResponseAsync(messages, options, cancellationToken))
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
        var userPrompt = $$"""
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

        var actions = await GetActionsAsync(
            messages,
            ChatToolMode.RequireSpecific("dm"),
            new ChatInputSafetyContext(player, [context.Text]),
            cancellationToken);
        if (actions is not [StarDirectMessageAction directMessage] ||
            directMessage.StarId != starId ||
            !ContentSafetyPolicy.IsOutputAllowed(directMessage.Text) ||
            !ContentSafetyPolicy.IsOutputAllowed(directMessage.MemoryFact))
        {
            throw new AiServiceUnavailableException("The model returned an invalid direct message.");
        }

        return new GeneratedDirectMessage(
            directMessage.Text,
            MemoryFactPolicy.IsAllowed(directMessage.MemoryFact) ? directMessage.MemoryFact : string.Empty);
    }

    private async ValueTask<IReadOnlyList<IStarAction>> GetActionsAsync(
        IReadOnlyList<ChatMessage> messages,
        ChatToolMode toolMode,
        ChatInputSafetyContext safetyContext,
        CancellationToken cancellationToken)
    {
        var options = CreateOptions(toolMode, safetyContext, StarToolSchemas.DirectMessage);
        try
        {
            var response = await chatClient.GetResponseAsync(
                messages,
                options,
                cancellationToken);
            var calls = response.Messages
                .SelectMany(message => message.Contents)
                .OfType<FunctionCallContent>();
            return StarToolCallParser.Parse(calls);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AiServiceUnavailableException("The model request timed out.", exception);
        }
        catch (Exception exception) when (exception is not (
                                              OperationCanceledException or
                                              AiServiceUnavailableException or
                                              ContentRejectedException or
                                              CrisisDetectedException))
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
            Temperature = settings.Value.Temperature,
            MaxOutputTokens = settings.Value.MaxOutputTokens,
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
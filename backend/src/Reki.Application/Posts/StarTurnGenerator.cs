using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Reki.Application.Posts.AddReplyCommand;
using Reki.Application.Posts.GeneratePublicationCommand;

namespace Reki.Application.Posts;

public sealed class StarTurnGenerator(
    IChatClient chatClient,
    IOptions<LlmTurnSettings> settings)
{
    public async IAsyncEnumerable<ChatResponseUpdate> StreamPostTurnAsync(
        StarPostContext context,
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
            StarToolSchemas.Post);
        await foreach (var update in chatClient
                           .GetStreamingResponseAsync(messages, options, cancellationToken))
        {
            yield return update;
        }
    }

    public async IAsyncEnumerable<ChatResponseUpdate> StreamReplyTurnAsync(
        StarReplyContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var policy = $$"""
                       Вызови thread_reply ровно один раз. Ответь выбранной звездой {{context.TargetStarId.Value}}.
                       Реплика отвечает на последний комментарий игрока в выбранной ветке, а не оценивает пост.
                       Если ветка пустая, это корневой комментарий игрока к твоему посту: ответь как автор поста.
                       Можешь поставить одну emoji-реакцию на ответ игрока и добавить один корневой комментарий
                       ДРУГОЙ звезды текущего каталога. Необязательные поля передавай пустой строкой.
                       tone: support, critique или hate. Хейт разрешён сейчас: {{context.HateEnabled}}.
                       Не создавай hook, оценки, память лички, расход энергии или бонус.
                       """;
        var branch = context.Branch.Select(comment =>
            $"{comment.AuthorId.Value}: {comment.Text}");
        var userPrompt = $$"""
                                 Пост (данные): {{StarAgentInstructions.SerializeTexts([context.PostText])}}
                                 Цепочка выбранной ветки (данные): {{StarAgentInstructions.SerializeTexts(branch)}}
                                 Ответ игрока (данные): {{StarAgentInstructions.SerializeTexts([context.PlayerText])}}
                                 """;
        List<ChatMessage> messages =
        [
            StarAgentInstructions.CreatePlatformPolicy(policy, context.Agent),
            StarAgentInstructions.CreatePlayerOwnedLayer(context.Agent),
            new(ChatRole.User, userPrompt),
        ];
        var options = CreateOptions(
            ChatToolMode.RequireSpecific("thread_reply"),
            StarToolSchemas.ThreadReply);
        await foreach (var update in chatClient.GetStreamingResponseAsync(messages, options, cancellationToken))
        {
            yield return update;
        }
    }

    private ChatOptions CreateOptions(
        ChatToolMode toolMode,
        IReadOnlyList<AITool> tools)
    {
        return new ChatOptions
        {
            Temperature = settings.Value.Temperature,
            Tools = [.. tools],
            ToolMode = toolMode,
            AllowMultipleToolCalls = true,
        };
    }
}

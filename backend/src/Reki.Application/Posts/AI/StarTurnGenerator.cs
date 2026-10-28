using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Reki.Application.Posts.AddReplyCommand;
using Reki.Application.Posts.GeneratePublicationCommand;

namespace Reki.Application.Posts.AI;

internal sealed class StarTurnGenerator(
    IChatClient chatClient,
    StarAgentInstructions instructions,
    IOptions<LlmTurnSettings> settings) : IStarTurnGenerator
{
    [StringSyntax(StringSyntaxAttribute.Json)]
    private const string ThreadReplySchema =
        """
        {
          "type": "object",
          "properties": {
            "star_id": { "type": "string", "minLength": 1, "maxLength": 64 },
            "text": { "type": "string", "minLength": 1, "maxLength": 500 },
            "tone": { "type": "string", "enum": ["support", "critique", "hate"] },
            "emoji": { "type": "string", "maxLength": 8 },
            "additional_star_id": { "type": "string", "maxLength": 64 },
            "additional_text": { "type": "string", "maxLength": 500 },
            "additional_tone": { "type": "string", "enum": ["", "support", "critique", "hate"] }
          },
          "required": ["star_id", "text", "tone", "emoji", "additional_star_id", "additional_text", "additional_tone"],
          "additionalProperties": false
        }
        """;

    [StringSyntax(StringSyntaxAttribute.Json)]
    private const string ReactSchema =
        """
        {
          "type": "object",
          "properties": {
            "emoji": { "type": "string", "minLength": 1, "maxLength": 8 }
          },
          "required": ["emoji"],
          "additionalProperties": false
        }
        """;

    [StringSyntax(StringSyntaxAttribute.Json)]
    private const string CommentSchema =
        """
        {
          "type": "object",
          "properties": {
            "author": { "type": "string", "description": "Точное отображаемое name персонажа из каталога, не id.", "minLength": 1, "maxLength": 40 },
            "star_id": { "type": "string", "minLength": 1, "maxLength": 40 },
            "text": { "type": "string", "minLength": 1, "maxLength": 500 },
            "hook": { "type": "string", "minLength": 1, "maxLength": 240 },
            "tone": { "type": "string", "enum": ["support", "critique", "hate"] },
            "brave": { "type": "number", "minimum": 0, "maximum": 1 },
            "humor": { "type": "number", "minimum": 0, "maximum": 1 },
            "topic": { "type": "number", "minimum": 0, "maximum": 1 },
            "why": {
              "type": "array",
              "minItems": 1,
              "maxItems": 3,
              "items": {
                "type": "object",
                "properties": {
                  "icon": { "type": "string", "minLength": 1, "maxLength": 8 },
                  "label": { "type": "string", "minLength": 1, "maxLength": 80 }
                },
                "required": ["icon", "label"],
                "additionalProperties": false
              }
            },
            "on_topic": { "type": "boolean" }
          },
          "required": ["author", "star_id", "text", "hook", "tone", "brave", "humor", "topic", "why", "on_topic"],
          "additionalProperties": false
        }
        """;

    private static readonly IReadOnlyList<AITool> Post =
    [
        Create("react", "Поставить реакцию на пост.", ReactSchema),
        Create(
            "comment",
            "Оценить пост и оставить комментарий звезды. Экономику считает сервер.",
            CommentSchema),
    ];

    private static readonly IReadOnlyList<AITool> ThreadReply =
        [Create("thread_reply", "Ответить в треде; необязательные поля передаются пустой строкой.", ThreadReplySchema)];

    public async IAsyncEnumerable<ChatResponseUpdate> StreamPostTurnAsync(
        StarPostContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var recentPosts = context.RecentPosts.Count == 0
            ? "нет"
            : instructions.SerializeTexts(context.RecentPosts);
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
            instructions.CreatePlatformPolicy(turnPolicy, context.Agent),
            instructions.CreatePlayerOwnedLayer(context.Agent),
            new(ChatRole.User, userPrompt),
        ];
        var options = CreateOptions(
            ChatToolMode.RequireSpecific("comment"),
            Post);
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
                                 Пост (данные): {{instructions.SerializeTexts([context.PostText])}}
                                 Цепочка выбранной ветки (данные): {{instructions.SerializeTexts(branch)}}
                                 Ответ игрока (данные): {{instructions.SerializeTexts([context.PlayerText])}}
                                 """;
        List<ChatMessage> messages =
        [
            instructions.CreatePlatformPolicy(policy, context.Agent),
            instructions.CreatePlayerOwnedLayer(context.Agent),
            new(ChatRole.User, userPrompt),
        ];
        var options = CreateOptions(
            ChatToolMode.RequireSpecific("thread_reply"),
            ThreadReply);
        await foreach (var update in chatClient.GetStreamingResponseAsync(messages, options, cancellationToken))
        {
            yield return update;
        }
    }

    private static AIFunctionDeclaration Create(string name, string description, string schema)
    {
        using var document = JsonDocument.Parse(schema);
        return AIFunctionFactory.CreateDeclaration(
            name,
            description,
            document.RootElement.Clone());
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

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Reki.Application.Posts.GeneratePublicationCommand;
using Reki.Application.Posts.GenerateReplyCommand;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.AI;

internal sealed class StarTurnGenerator(
    IChatClient chatClient,
    IStarAgentInstructions instructions,
    IOptions<LlmTurnSettings> settings) : IStarTurnGenerator
{
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
            "star_id": { "type": "string", "minLength": 1, "maxLength": 40 },
            "text": { "type": "string", "minLength": 1, "maxLength": 500 },
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
          "required": ["star_id", "text", "tone", "brave", "humor", "topic", "why", "on_topic"],
          "additionalProperties": false
        }
        """;

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
                           star_id — поле id персонажа из world.stars. Имя автора определяет сервер.
                           Выбирай только персонажей world.stars, не персонажа игрока world.player.
                           tone: support (поддержка), critique (критика текста) или hate (острая реакция на текст).
                           Хейт разрешён сейчас: {{context.HateEnabled}}. Если false, tone=hate запрещён.
                           """;
        var userPrompt = $$"""
                           Этап: {{context.Stage}}
                           Автор новых и предыдущих постов — персонаж игрока world.player (id={{context.Agent.World.Player.PlayerId.ToAuthorId().Value}}).
                           Последние посты персонажа игрока:
                           {{recentPosts}}

                           Новый пост персонажа игрока, на который отвечает звезда:
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
            CreatePostTools(context.Agent.World));
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
                       Ответь обычным текстом от лица выбранной звезды {{context.TargetStarId.Value}}.
                       Реплика отвечает на последний комментарий игрока в выбранной ветке, а не оценивает пост.
                       Если последний текст совпадает с постом, напиши первый комментарий к этому посту.
                       Иначе при пустой ветке ответь на корневой комментарий игрока к твоему посту.
                       Это дополнительная звезда: {{context.Additional}}. Если true, напиши самостоятельную
                       корневую реплику к посту с учётом комментария игрока.
                       Хейт разрешён сейчас: {{context.HateEnabled}}.
                       Верни только текст реплики до 500 символов. Не вызывай инструменты и не создавай
                       оценки, память лички, расход энергии или бонус.
                       """;
        var world = context.Agent.World;
        var branch = context.Branch.Select(comment =>
            $"{comment.AuthorId.GetAuthorNamePolicy(world.FindStar(comment.AuthorId)?.Name ?? "вне текущего каталога", world.Player.Name)}: {comment.Text}");
        var userPrompt = $$"""
                                 Пост (данные): {{instructions.SerializeTexts([context.PostText])}}
                                 Цепочка выбранной ветки (данные): {{instructions.SerializeTexts(branch)}}
                                 Последний комментарий персонажа игрока world.player (id={{world.Player.PlayerId.ToAuthorId().Value}}, данные): {{instructions.SerializeTexts([context.PlayerText])}}
                                 """;
        List<ChatMessage> messages =
        [
            instructions.CreatePlatformPolicy(policy, context.Agent),
            instructions.CreatePlayerOwnedLayer(context.Agent),
            new(ChatRole.User, userPrompt),
        ];
        var options = new ChatOptions { Temperature = settings.Value.Temperature };
        await foreach (var update in chatClient.GetStreamingResponseAsync(messages, options, cancellationToken))
        {
            yield return update;
        }
    }

    private static IReadOnlyList<AITool> CreatePostTools(World world) =>
    [
        Create("react", "Поставить реакцию на пост.", ReactSchema),
        Create(
            "comment",
            "Оценить пост и оставить комментарий звезды. Экономику считает сервер.",
            CreateStarSchema(CommentSchema, world.Stars.Select(star => star.Id.Value))),
    ];

    private static string CreateStarSchema(
        string schema,
        IEnumerable<string> starIds)
    {
        var document = JsonNode.Parse(schema)
            ?? throw new InvalidOperationException("The tool schema is missing.");
        var properties = document["properties"]
            ?? throw new InvalidOperationException("The tool schema properties are missing.");
        var starProperty = properties["star_id"]
            ?? throw new InvalidOperationException("The tool schema star identity is missing.");
        starProperty["enum"] = CreateAllowedIds(starIds);
        return document.ToJsonString();
    }

    private static JsonArray CreateAllowedIds(IEnumerable<string> starIds)
    {
        var values = new JsonArray();
        foreach (var starId in starIds)
        {
            values.Add(JsonValue.Create(starId));
        }

        return values;
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

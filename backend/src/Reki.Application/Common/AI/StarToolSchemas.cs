using System.Text.Json;
using Microsoft.Extensions.AI;

namespace Reki.Application.Common.AI;

internal static class StarToolSchemas
{
    public static readonly IReadOnlyList<AITool> All =
    [
        Create("react", "Поставить реакцию на пост.", ReactSchema),
        Create(
            "comment",
            "Оценить пост и оставить комментарий звезды. Экономику считает сервер.",
            CommentSchema),
        Create("repost", "Сделать репост от имени звезды.", RepostSchema),
        Create(
            "dm",
            "Ответить игроку в личных сообщениях и запомнить один безопасный факт.",
            DirectMessageSchema),
        Create(
            "start_event",
            "Начать событие разрешённого типа из правил мира.",
            StartEventSchema),
    ];

    public static readonly IReadOnlyList<AITool> Post = All.Where(tool => tool.Name is "comment" or "react").ToArray();

    public static readonly IReadOnlyList<AITool> DirectMessage = All.Where(tool => tool.Name == "dm").ToArray();

    private const string ReactSchema = """
        {
          "type": "object",
          "properties": {
            "emoji": { "type": "string", "minLength": 1, "maxLength": 8 }
          },
          "required": ["emoji"],
          "additionalProperties": false
        }
        """;

    private const string CommentSchema = """
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

    private const string RepostSchema = """
        {
          "type": "object",
          "properties": {
            "star_id": { "type": "string", "minLength": 1, "maxLength": 40 },
            "text": { "type": "string", "minLength": 1, "maxLength": 500 }
          },
          "required": ["star_id", "text"],
          "additionalProperties": false
        }
        """;

    private const string DirectMessageSchema = """
        {
          "type": "object",
          "properties": {
            "star_id": { "type": "string", "minLength": 1, "maxLength": 40 },
            "text": { "type": "string", "minLength": 1, "maxLength": 1000 },
            "memory_fact": { "type": "string", "maxLength": 160 }
          },
          "required": ["star_id", "text", "memory_fact"],
          "additionalProperties": false
        }
        """;

    private const string StartEventSchema = """
        {
          "type": "object",
          "properties": {
            "type": { "type": "string", "enum": ["collab", "rumor", "chance", "hate"] },
            "text": { "type": "string", "minLength": 1, "maxLength": 1000 }
          },
          "required": ["type", "text"],
          "additionalProperties": false
        }
        """;

    private static AIFunctionDeclaration Create(string name, string description, string schema)
    {
        using JsonDocument document = JsonDocument.Parse(schema);
        return AIFunctionFactory.CreateDeclaration(
            name,
            description,
            document.RootElement.Clone());
    }
}

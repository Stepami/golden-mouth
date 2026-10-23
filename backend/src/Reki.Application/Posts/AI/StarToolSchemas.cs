using System.Text.Json;
using Microsoft.Extensions.AI;

namespace Reki.Application.Posts.AI;

internal static class StarToolSchemas
{
    public static readonly IReadOnlyList<AITool> Post =
    [
        Create("react", "Поставить реакцию на пост.", ReactSchema),
        Create(
            "comment",
            "Оценить пост и оставить комментарий звезды. Экономику считает сервер.",
            CommentSchema),
    ];

    public static readonly IReadOnlyList<AITool> ThreadReply =
        [Create("thread_reply", "Ответить в треде; необязательные поля передаются пустой строкой.", ThreadReplySchema)];

    private const string ThreadReplySchema = """
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

    private static AIFunctionDeclaration Create(string name, string description, string schema)
    {
        using var document = JsonDocument.Parse(schema);
        return AIFunctionFactory.CreateDeclaration(
            name,
            description,
            document.RootElement.Clone());
    }
}

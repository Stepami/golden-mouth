using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;
using Reki.Application.Common.Exceptions;
using Reki.Domain.Player;

namespace Reki.Application.Common.Safety;

public sealed partial class ContentModerator(IChatClient chatClient) : IContentModerator
{
    private static readonly HashSet<string> Categories =
        ["allowed", "age_restricted_topic", "harassment", "hate", "dangerous_instructions", "explicit_content", "crisis", "uncertain"];

    private static readonly AIFunctionDeclaration Tool = CreateTool();

    public async ValueTask<ContentReview> ReviewAsync(PlayerProfile player, IReadOnlyList<string> texts, CancellationToken cancellationToken)
    {
        if (texts.All(string.IsNullOrWhiteSpace))
        {
            return new ContentReview(SafetyDecision.Allowed, "allowed");
        }

        List<ChatMessage> messages =
        [
            new(ChatRole.System, $"{PublicationSafetyPolicy.Instructions}\nВозрастной режим: {PublicationSafetyPolicy.AgeMode(player)}. Версия: {PublicationSafetyPolicy.Version}."),
            new(ChatRole.User, JsonSerializer.Serialize(texts, ContentReviewJsonContext.Default.IReadOnlyListString)),
        ];
        var options = new ChatOptions
        {
            Tools = [Tool],
            ToolMode = ChatToolMode.RequireSpecific("content_review"),
            Temperature = 0,
        };
        try
        {
            var response = await chatClient.GetResponseAsync(messages, options, cancellationToken);
            var call = response.Messages.SelectMany(message => message.Contents).OfType<FunctionCallContent>().Single();
            if (call.Name != "content_review" || call.Arguments?.TryGetValue("category", out var raw) != true ||
                raw?.ToString() is not { } category || !Categories.Contains(category))
            {
                throw new AiServiceUnavailableException("Content review returned an invalid decision.");
            }

            var decision = category switch
            {
                "allowed" => SafetyDecision.Allowed,
                "crisis" => SafetyDecision.Crisis,
                _ => SafetyDecision.Rejected,
            };
            return new ContentReview(decision, category);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            throw new AiServiceUnavailableException("Content review is unavailable.", exception);
        }
    }

    private static AIFunctionDeclaration CreateTool()
    {
        using var schema = JsonDocument.Parse("""
            {"type":"object","properties":{"category":{"type":"string","enum":["allowed","age_restricted_topic","harassment","hate","dangerous_instructions","explicit_content","crisis","uncertain"]}},"required":["category"],"additionalProperties":false}
            """);
        return AIFunctionFactory.CreateDeclaration("content_review", "Проверить содержание по возрастной политике", schema.RootElement.Clone());
    }

    [JsonSerializable(typeof(IReadOnlyList<string>))]
    private sealed partial class ContentReviewJsonContext : JsonSerializerContext;
}

using Microsoft.Extensions.AI;
using OpenAI.Chat;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace Reki.Infrastructure.AI;

/// <summary>
/// DeepSeek включает режим размышления по умолчанию, а в нём запрещён принудительный выбор
/// инструмента, на котором построен ход звезды. Для моделей DeepSeek из списка middleware дописывает
/// в опции запроса <c>"thinking": {"type": "disabled"}</c>; остальные модели не затронуты.
/// </summary>
internal sealed class DeepSeekThinkingChatClient(IChatClient innerClient) : DelegatingChatClient(innerClient)
{
    private static readonly HashSet<string> Models = new(StringComparer.OrdinalIgnoreCase)
    {
        "deepseek-flash",
        "deepseek-v4-flash",
        "deepseek-v4-pro",
    };

    public override Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default) =>
        base.GetResponseAsync(messages, WithThinkingDisabled(options), cancellationToken);

    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default) =>
        base.GetStreamingResponseAsync(messages, WithThinkingDisabled(options), cancellationToken);

    private ChatOptions? WithThinkingDisabled(ChatOptions? options)
    {
        var modelId = options?.ModelId ?? this.GetService<ChatClientMetadata>()?.DefaultModelId;
        if (modelId is null || !Models.Contains(modelId))
        {
            return options;
        }

        var patched = options?.Clone() ?? new ChatOptions();
        var rawFactory = patched.RawRepresentationFactory;
        patched.RawRepresentationFactory = client =>
        {
            var completionOptions =
                rawFactory?.Invoke(client) as ChatCompletionOptions ?? new ChatCompletionOptions();
            completionOptions.Patch.Set("$.thinking"u8, BinaryData.FromString("""{"type":"disabled"}"""));
            return completionOptions;
        };
        return patched;
    }
}

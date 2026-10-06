using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace Reki.Infrastructure.AI;

/// <summary>
/// DeepSeek включает режим размышления по умолчанию, а в нём запрещён принудительный выбор
/// инструмента, на котором построен ход звезды. Для провайдера DeepSeek middleware дописывает
/// в опции запроса <c>"thinking": {"type": "disabled"}</c>; другие провайдеры не затронуты.
/// </summary>
internal sealed class DeepSeekThinkingChatClient(IChatClient innerClient) : DelegatingChatClient(innerClient)
{
    private const string ProviderMarker = "deepseek";

    public override Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default) =>
        base.GetResponseAsync(messages, WithThinkingDisabled(options), cancellationToken);

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (ChatResponseUpdate update in base.GetStreamingResponseAsync(
            messages,
            WithThinkingDisabled(options),
            cancellationToken))
        {
            yield return update;
        }
    }

    private ChatOptions? WithThinkingDisabled(ChatOptions? options)
    {
        Uri? providerUri = GetService<ChatClientMetadata>()?.ProviderUri;
        if (providerUri?.Host.Contains(ProviderMarker, StringComparison.OrdinalIgnoreCase) != true)
        {
            return options;
        }

        ChatOptions patched = options?.Clone() ?? new ChatOptions();
        Func<IChatClient, object?>? rawFactory = patched.RawRepresentationFactory;
        patched.RawRepresentationFactory = client =>
        {
            ChatCompletionOptions completionOptions =
                rawFactory?.Invoke(client) as ChatCompletionOptions ?? new ChatCompletionOptions();
#pragma warning disable SCME0001 // JsonPatch экспериментальный, другого способа передать поле вне схемы OpenAI нет.
            completionOptions.Patch.Set("$.thinking"u8, BinaryData.FromString("""{"type":"disabled"}"""));
#pragma warning restore SCME0001
            return completionOptions;
        };
        return patched;
    }
}

using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Reki.Application.Common.AI;
using Reki.Application.Common.Safety;

namespace Reki.Infrastructure.AI;

internal sealed class PlayerSafetyChatClient(
    IChatClient innerClient,
    PlayerSafetyGuard safetyGuard) : DelegatingChatClient(innerClient)
{
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var providerOptions = await CheckInputAsync(options, cancellationToken);
        return await base.GetResponseAsync(messages, providerOptions, cancellationToken);
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var providerOptions = await CheckInputAsync(options, cancellationToken);
        await foreach (var update in base.GetStreamingResponseAsync(
            messages,
            providerOptions,
            cancellationToken))
        {
            yield return update;
        }
    }

    private async ValueTask<ChatOptions> CheckInputAsync(
        ChatOptions? options,
        CancellationToken cancellationToken)
    {
        if (options?.AdditionalProperties?.TryGetValue(
            ChatInputSafetyContext.OptionKey,
            out var contextValue) is not true ||
            contextValue is not ChatInputSafetyContext context)
        {
            throw new InvalidOperationException("The chat request is missing input safety context.");
        }

        await safetyGuard.EnsureInputAllowedAsync(context.Player, context.Texts, cancellationToken);
        var providerOptions = options.Clone();
        providerOptions.AdditionalProperties?.Remove(ChatInputSafetyContext.OptionKey);
        return providerOptions;
    }
}

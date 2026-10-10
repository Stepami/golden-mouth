using Microsoft.Extensions.AI;
using NSubstitute;
using OpenAI.Chat;
using Reki.Infrastructure.AI;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace Reki.Tests.AI;

public sealed class DeepSeekThinkingChatClientTests
{
    [Test]
    public async Task GetResponse_DeepSeekModel_DisablesThinkingAsync()
    {
        // Arrange
        var inner = CreateInner();
        using var client = new DeepSeekThinkingChatClient(inner);

        // Act
        await client.GetResponseAsync([], new ChatOptions { ModelId = "deepseek-flash" });

        // Assert
        var sent = SentOptions(inner);
        var completionOptions = sent?.RawRepresentationFactory?.Invoke(inner) as ChatCompletionOptions;
#pragma warning disable SCME0001 // JsonPatch экспериментальный.
        var hasThinking = completionOptions?.Patch.Contains("$.thinking"u8) ?? false;
#pragma warning restore SCME0001
        await Assert.That(hasThinking).IsTrue();
        await Assert.That(sent?.ModelId).IsEqualTo("deepseek-flash");
    }

    [Test]
    public async Task GetResponse_OtherModel_KeepsOptionsAsync()
    {
        // Arrange
        var inner = CreateInner();
        using var client = new DeepSeekThinkingChatClient(inner);
        var options = new ChatOptions { ModelId = "ai/gemma4" };

        // Act
        await client.GetResponseAsync([], options);

        // Assert
        await Assert.That(SentOptions(inner)).IsSameReferenceAs(options);
    }

    private static IChatClient CreateInner()
    {
        var inner = Substitute.For<IChatClient>();
        inner.GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(new ChatResponse());
        return inner;
    }

    private static ChatOptions? SentOptions(IChatClient inner) =>
        inner.ReceivedCalls()
            .Single(call => call.GetMethodInfo().Name == nameof(IChatClient.GetResponseAsync))
            .GetArguments()[1] as ChatOptions;
}

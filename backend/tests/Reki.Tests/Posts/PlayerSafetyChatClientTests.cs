using Microsoft.Extensions.AI;
using NSubstitute;
using Reki.Application.Common;
using Reki.Application.Common.AI;
using Reki.Application.Common.Exceptions;
using Reki.Application.Common.Safety;
using Reki.Infrastructure.AI;
using Reki.Tests.Common;

namespace Reki.Tests.Posts;

public sealed class PlayerSafetyChatClientTests
{
    [Test]
    public async Task GetResponse_StripsCheckedPlayerContextAsync()
    {
        var inner = Substitute.For<IChatClient>();
        var store = Substitute.For<IGameStore>();
        ChatOptions? forwarded = null;
        inner.GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                forwarded = callInfo.ArgAt<ChatOptions?>(1);
                return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));
            });
        var client = new PlayerSafetyChatClient(
            inner,
            new PlayerSafetyGuard(store, TimeProvider.System));
        var options = new ChatOptions
        {
            AdditionalProperties = new AdditionalPropertiesDictionary
            {
                [ChatInputSafetyContext.OptionKey] = new ChatInputSafetyContext(
                    TestGameData.CreatePlayer(),
                    ["обычный пост"]),
            },
        };

        await client.GetResponseAsync([new ChatMessage(ChatRole.User, "обычный пост")], options);

        await Assert.That(forwarded is not null).IsTrue();
        await Assert.That(forwarded?.AdditionalProperties?.ContainsKey(
            ChatInputSafetyContext.OptionKey) ?? false).IsFalse();
        await Assert.That(options.AdditionalProperties.ContainsKey(
            ChatInputSafetyContext.OptionKey)).IsTrue();
    }

    [Test]
    public async Task StreamingInput_IsCheckedWhenEnumeratedAsync()
    {
        var inner = Substitute.For<IChatClient>();
        var store = Substitute.For<IGameStore>();
        var client = new PlayerSafetyChatClient(
            inner,
            new PlayerSafetyGuard(store, TimeProvider.System));
        var options = new ChatOptions
        {
            AdditionalProperties = new AdditionalPropertiesDictionary
            {
                [ChatInputSafetyContext.OptionKey] = new ChatInputSafetyContext(
                    TestGameData.CreatePlayer(),
                    ["наркотики"]),
            },
        };
        var stream = client.GetStreamingResponseAsync(
            [new ChatMessage(ChatRole.User, "наркотики")],
            options);
        var rejected = false;
        var updates = 0;

        try
        {
            await foreach (var update in stream)
            {
                updates += update.Contents.Count;
            }
        }
        catch (ContentRejectedException)
        {
            rejected = true;
        }

        await Assert.That(rejected).IsTrue();
        await Assert.That(updates).IsEqualTo(0);
        _ = inner.DidNotReceive().GetStreamingResponseAsync(
            Arg.Any<IEnumerable<ChatMessage>>(),
            Arg.Any<ChatOptions?>(),
            Arg.Any<CancellationToken>());
    }
}

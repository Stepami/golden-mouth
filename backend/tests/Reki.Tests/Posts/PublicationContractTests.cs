using System.Text.Json;
using Microsoft.Extensions.AI;
using NSubstitute;
using Reki.Api.Serialization;
using Reki.Application.Common.AI;
using Reki.Application.Common.Exceptions;
using Reki.Application.Posts;
using Reki.Contracts.Posts;
using Reki.Domain.Worlds;
using Reki.Tests.Common;

namespace Reki.Tests.Posts;

public sealed class PublicationContractTests
{
    [Test]
    public async Task LiveEvent_UsesLowercaseTypeInActualSseSerializerAsync()
    {
        string json = JsonSerializer.Serialize(
            new ViewsLiveEvent(LiveEventType.Views, 48),
            ApiJsonSerializerContext.Default.ViewsLiveEvent);
        using JsonDocument document = JsonDocument.Parse(json);

        await Assert.That(document.RootElement.GetProperty("type").GetString()).IsEqualTo("views");
    }

    [Test]
    public async Task UnsafePostOutput_FailsWithoutAnotherGenerationAttemptAsync()
    {
        IChatClient chatClient = Substitute.For<IChatClient>();
        using JsonDocument reasons = JsonDocument.Parse("""[{"icon":"🔥","label":"тема"}]""");
        var arguments = new Dictionary<string, object?>
        {
            ["author"] = "Звезда",
            ["star_id"] = "sonya",
            ["text"] = "наркотики",
            ["hook"] = "обычное продолжение",
            ["brave"] = 0.5,
            ["humor"] = 0.5,
            ["topic"] = 0.5,
            ["why"] = reasons.RootElement.Clone(),
            ["on_topic"] = true,
        };
        ChatResponse response = new(new ChatMessage(
            ChatRole.Assistant,
            [new FunctionCallContent("call-1", "comment", arguments)]));
        chatClient.GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));
        var generator = new StarTurnGenerator(
            chatClient,
            new LlmTurnSettings("test", 0.4f, 700, TimeSpan.FromSeconds(5)));
        var context = new StarPostContext(
            "обычный пост",
            new StarAgentProfile(WorldId.From("stage"), "мир", "описание", [], "off", string.Empty, null, []),
            "mask",
            [],
            false);

        bool failed = false;
        try
        {
            await generator.GeneratePostTurnAsync(
                context,
                TestGameData.CreatePlayer(),
                CancellationToken.None);
        }
        catch (AiServiceUnavailableException)
        {
            failed = true;
        }

        await Assert.That(failed).IsTrue();
        await chatClient.Received(1).GetResponseAsync(
            Arg.Any<IEnumerable<ChatMessage>>(),
            Arg.Any<ChatOptions?>(),
            Arg.Any<CancellationToken>());
    }
}

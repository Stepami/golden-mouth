using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text.Json.Nodes;
using Reki.Infrastructure.AI;

namespace Reki.Tests.AI;

public sealed class DeepSeekThinkingPolicyTests
{
    [Test]
    public async Task AppliesTo_OnlyDeepSeekHostAsync()
    {
        await Assert.That(DeepSeekThinkingPolicy.AppliesTo(new Uri("https://api.deepseek.com"))).IsTrue();
        await Assert.That(DeepSeekThinkingPolicy.AppliesTo(new Uri("https://api.deepseek.com/v1"))).IsTrue();
        await Assert.That(DeepSeekThinkingPolicy.AppliesTo(new Uri("http://model-runner.docker.internal/engines/v1/"))).IsFalse();
    }

    [Test]
    public async Task DisableThinking_ChatRequest_AddsDisabledThinkingAsync()
    {
        // Arrange
        PipelineRequest request = CreateRequest("""{"model":"deepseek-flash","messages":[],"tool_choice":"required"}""");

        // Act
        DeepSeekThinkingPolicy.DisableThinking(request);

        // Assert
        JsonObject body = ReadBody(request);
        await Assert.That(body["thinking"]?["type"]?.GetValue<string>()).IsEqualTo("disabled");
        await Assert.That(body["tool_choice"]?.GetValue<string>()).IsEqualTo("required");
    }

    [Test]
    public async Task DisableThinking_NonChatRequest_KeepsBodyAsync()
    {
        // Arrange
        PipelineRequest request = CreateRequest("""{"input":"text"}""");

        // Act
        DeepSeekThinkingPolicy.DisableThinking(request);

        // Assert
        await Assert.That(ReadBody(request).ContainsKey("thinking")).IsFalse();
    }

    private static PipelineRequest CreateRequest(string json)
    {
        PipelineRequest request = ClientPipeline.Create().CreateMessage().Request;
        request.Content = BinaryContent.Create(BinaryData.FromString(json));
        return request;
    }

    private static JsonObject ReadBody(PipelineRequest request)
    {
        using var buffer = new MemoryStream();
        request.Content?.WriteTo(buffer);
        return JsonNode.Parse(buffer.ToArray()) as JsonObject ?? [];
    }
}

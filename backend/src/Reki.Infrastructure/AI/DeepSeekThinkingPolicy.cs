using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text.Json.Nodes;

namespace Reki.Infrastructure.AI;

/// <summary>
/// DeepSeek включает режим размышления по умолчанию, а в нём запрещён принудительный выбор
/// инструмента, на котором построен ход звезды. Политика дописывает в тело chat-запроса
/// <c>"thinking": {"type": "disabled"}</c>. Подключается только для хоста DeepSeek.
/// </summary>
internal sealed class DeepSeekThinkingPolicy : PipelinePolicy
{
    private const string DeepSeekHost = "api.deepseek.com";

    public static bool AppliesTo(Uri endpoint) =>
        endpoint.Host.Equals(DeepSeekHost, StringComparison.OrdinalIgnoreCase);

    public override void Process(
        PipelineMessage message,
        IReadOnlyList<PipelinePolicy> pipeline,
        int currentIndex)
    {
        DisableThinking(message.Request);
        ProcessNext(message, pipeline, currentIndex);
    }

    public override async ValueTask ProcessAsync(
        PipelineMessage message,
        IReadOnlyList<PipelinePolicy> pipeline,
        int currentIndex)
    {
        DisableThinking(message.Request);
        await ProcessNextAsync(message, pipeline, currentIndex);
    }

    internal static void DisableThinking(PipelineRequest request)
    {
        if (request.Content is null)
        {
            return;
        }

        using var buffer = new MemoryStream();
        request.Content.WriteTo(buffer);
        if (JsonNode.Parse(buffer.ToArray()) is not JsonObject body || !body.ContainsKey("messages"))
        {
            return;
        }

        body["thinking"] = new JsonObject { ["type"] = "disabled" };
        request.Content = BinaryContent.Create(BinaryData.FromString(body.ToJsonString()));
    }
}

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Reki.Application.Posts.AI.Safety;
using Reki.Application.Telemetry;

namespace Reki.Application.Posts.AI;

internal sealed class StarContentGenerator(
    IChatClient chatClient,
    IStarAgentInstructions instructions,
    IOptions<LlmTurnSettings> settings,
    ProductTelemetry telemetry) : IStarContentGenerator
{
    public async ValueTask<string?> GenerateContentAsync(IStarFeedContext context, CancellationToken cancellationToken)
    {
        var messages = context.GetChatMessages(instructions);
        telemetry.TrackLlmRequest(context.Agent.World.Player.PlayerId, "star_content", context.Agent.StarId?.ToString() ?? string.Empty);
        var response = await chatClient.GetResponseAsync(
            messages,
            new ChatOptions { Temperature = settings.Value.Temperature },
            cancellationToken);
        var text = response.Text.Trim();
        return !string.IsNullOrWhiteSpace(text) && text.Length <= 1000 && PlayerSafetyGuard.IsOutputAllowed(text)
            ? text
            : null;
    }
}

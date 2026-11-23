using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Reki.Infrastructure.Auth;
using Reki.Infrastructure.Telegram.Command;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Reki.Api.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Authorize(AuthenticationSchemes = AuthConstants.Schemes.TelegramWebhook)]
[Route("/telegram")]
public sealed partial class TelegramController(
    ILogger<TelegramController> logger,
    IServiceProvider services) : ControllerBase
{
    [HttpPost("webhook")]
    public async Task<Ok> Webhook([FromBody] Update update)
    {
        if (update.Message is not { Chat.Type: ChatType.Private, From: not null, Text: { } text } message ||
            !text.StartsWith('/'))
        {
            LogUnsupportedUpdate();
            return TypedResults.Ok();
        }

        var command = text[1..];
        if (!BotCommands.All.Select(botCmd => botCmd.Command).Contains(command))
        {
            LogUnsupportedBotCommand(command);
            return TypedResults.Ok();
        }

        await services.GetRequiredKeyedService<IBotCommandHandler>(text[1..]).Handle(message);
        return TypedResults.Ok();
    }

    [LoggerMessage(LogLevel.Warning, "Unsupported update")]
    private partial void LogUnsupportedUpdate();

    [LoggerMessage(LogLevel.Warning, "Unsupported bot command {BotCommand}")]
    private partial void LogUnsupportedBotCommand(string botCommand);
}

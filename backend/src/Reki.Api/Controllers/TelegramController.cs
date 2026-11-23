using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Reki.Infrastructure.Auth;
using Reki.Infrastructure.Telegram.Command;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;

namespace Reki.Api.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Authorize(AuthenticationSchemes = AuthConstants.Schemes.TelegramWebhook)]
[Route("/telegram")]
public sealed partial class TelegramController(
    ILogger<TelegramController> logger,
    IServiceProvider services,
    ITelegramBotClient telegram) : ControllerBase
{
    [HttpPost("webhook")]
    public async Task<Ok> Webhook([FromBody] Update update)
    {
        if (update.InlineQuery is { } inlineQuery)
        {
            var query = inlineQuery.Query;
            var results = query.StartsWith(BotCommands.StoryInlinePrefix, StringComparison.Ordinal) && query.Length > BotCommands.StoryInlinePrefix.Length
                ? new InlineQueryResult[] { new InlineQueryResultCachedPhoto(inlineQuery.Id, query[BotCommands.StoryInlinePrefix.Length..]) }
                : [];
            await telegram.AnswerInlineQuery(inlineQuery.Id, results, isPersonal: true);
            return TypedResults.Ok();
        }

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

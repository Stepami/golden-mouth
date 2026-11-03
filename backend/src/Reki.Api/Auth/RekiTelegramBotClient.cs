using Microsoft.Extensions.Options;
using Reki.Infrastructure.Auth;
using Telegram.Bot;

namespace Reki.Api.Auth;

internal sealed class RekiTelegramBotClient(
    IOptions<TelegramOptions> telegramOptions,
    HttpClient httpClient) : TelegramBotClient(
        new TelegramBotClientOptions(
            telegramOptions.Value.BotToken,
            telegramOptions.Value.ApiUrl),
        httpClient);

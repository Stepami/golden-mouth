using Microsoft.Extensions.Options;
using Telegram.Bot;

namespace Reki.Infrastructure.Auth;

internal sealed class RekiTelegramBotClient(
    IOptions<TelegramOptions> telegramOptions,
    HttpClient httpClient) : TelegramBotClient(
        new TelegramBotClientOptions(
            telegramOptions.Value.BotToken,
            telegramOptions.Value.ApiUrl),
        httpClient);

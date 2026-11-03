using Microsoft.Extensions.Options;
using Telegram.Bot;

namespace Reki.Infrastructure.Telegram;

internal sealed class RekiTelegramBotClient(
    IOptions<TelegramOptions> telegramOptions,
    HttpClient httpClient) : TelegramBotClient(
        new TelegramBotClientOptions(
            telegramOptions.Value.BotToken,
            telegramOptions.Value.ApiUrl),
        httpClient);

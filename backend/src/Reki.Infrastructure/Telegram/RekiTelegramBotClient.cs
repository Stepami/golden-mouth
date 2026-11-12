using Microsoft.Extensions.Options;
using Telegram.Bot;

namespace Reki.Infrastructure.Telegram;

internal sealed class RekiTelegramBotClient(
    IOptions<TelegramBotClientOptions> telegramOptions,
    HttpClient httpClient) : TelegramBotClient(
    telegramOptions.Value,
    httpClient);
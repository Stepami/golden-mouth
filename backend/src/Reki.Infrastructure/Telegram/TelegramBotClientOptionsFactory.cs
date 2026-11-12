using Microsoft.Extensions.Options;
using Telegram.Bot;

namespace Reki.Infrastructure.Telegram;

internal sealed class TelegramBotClientOptionsFactory(
    IOptions<TelegramOptions> telegramOptions,
    IEnumerable<IConfigureOptions<TelegramBotClientOptions>> setups,
    IEnumerable<IPostConfigureOptions<TelegramBotClientOptions>> postConfigures,
    IEnumerable<IValidateOptions<TelegramBotClientOptions>> validations)
    : OptionsFactory<TelegramBotClientOptions>(setups, postConfigures, validations)
{
    protected override TelegramBotClientOptions CreateInstance(string name) =>
        new(telegramOptions.Value.BotToken, telegramOptions.Value.ApiUrl);
}
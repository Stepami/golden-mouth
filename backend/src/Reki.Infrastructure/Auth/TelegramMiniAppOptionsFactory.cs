using Microsoft.Extensions.Options;
using TgMiniAppAuth.Authentication;

namespace Reki.Infrastructure.Auth;

internal sealed class TelegramMiniAppOptionsFactory(
    IOptions<TelegramOptions> telegramOptions,
    IEnumerable<IConfigureOptions<TelegramMiniAppAuthorizationOptions>> setups,
    IEnumerable<IPostConfigureOptions<TelegramMiniAppAuthorizationOptions>> postConfigures,
    IEnumerable<IValidateOptions<TelegramMiniAppAuthorizationOptions>> validations)
    : OptionsFactory<TelegramMiniAppAuthorizationOptions>(setups, postConfigures, validations)
{
    protected override TelegramMiniAppAuthorizationOptions CreateInstance(string name) => new()
    {
        Token = telegramOptions.Value.BotToken,
    };
}

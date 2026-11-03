using Microsoft.Extensions.Options;
using Reki.Infrastructure.Auth;
using Telegram.Bot;
using TgMiniAppAuth.Authentication;

namespace Reki.Api.Auth;

internal sealed class RekiTelegramBotClient(
    IOptions<TelegramMiniAppAuthorizationOptions> tgMiniAppOptions,
    IOptions<AuthOptions> authOptions,
    HttpClient httpClient) : TelegramBotClient(
        new TelegramBotClientOptions(
            tgMiniAppOptions.Value.Token,
            authOptions.Value.TelegramBotApiUrl),
        httpClient);

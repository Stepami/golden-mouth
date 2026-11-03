namespace Reki.Api.Auth;

internal static class AuthConstants
{
    public const string AppJwtPolicy = "RekiAppJwt";
    public const string MiniAppGrantPolicy = "RekiMiniAppGrant";
    public const string TelegramLoginGrantPolicy = "RekiTelegramLoginGrant";
    public const string RefreshGrantPolicy = "RekiRefreshGrant";
    public const string MiniAppGrant = "urn:reki:grant:telegram-mini-app";
    public const string TelegramLoginGrant = "urn:reki:grant:telegram-login";
    public const string TelegramLoginScheme = "TelegramLogin";
    public const string TelegramUserIdClaim = "telegram_user_id";
}

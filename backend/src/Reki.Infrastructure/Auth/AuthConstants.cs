namespace Reki.Infrastructure.Auth;

public static class AuthConstants
{
    public static class Grants
    {
        public const string MiniApp = "urn:reki:grant:telegram-mini-app";
        public const string TelegramLogin = "urn:reki:grant:telegram-login";
        public const string Refresh = OpenIddict.Abstractions.OpenIddictConstants.GrantTypes.RefreshToken;
    }

    public static class Policies
    {
        public const string RekiAppJwt = nameof(RekiAppJwt);
        public const string RekiMiniAppGrant = nameof(RekiMiniAppGrant);
        public const string MiniApp = Schemes.MiniApp;
        public const string RekiTelegramLoginGrant = nameof(RekiTelegramLoginGrant);
        public const string RekiRefreshGrant = nameof(RekiRefreshGrant);
    }

    public static class Schemes
    {
        public const string MiniApp = TgMiniAppAuth.TgMiniAppAuthConstants.AuthenticationScheme;
        public const string TelegramLogin = nameof(TelegramLogin);
        public const string AppBearer = nameof(AppBearer);
    }

    public static class Claims
    {
        public const string TelegramUserId = "telegram_user_id";
    }

    public static class Issuers
    {
        public const string Telegram = "https://oauth.telegram.org";
    }
}

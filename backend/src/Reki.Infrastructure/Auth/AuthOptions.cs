namespace Reki.Infrastructure.Auth;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = "reki-api";

    public string ClientId { get; set; } = "reki-app";

    public string TelegramMetadataProxyUrl { get; set; } = string.Empty;

    public string TelegramBotApiUrl { get; set; } = string.Empty;

    public string SigningKey { get; set; } = string.Empty;

    public string EncryptionKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;

    public int RefreshTokenDays { get; set; } = 30;
}

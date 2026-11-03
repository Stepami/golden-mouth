namespace Reki.Infrastructure.Auth;

public sealed class TelegramOptions
{
    public const string SectionName = "Telegram";

    public string OAuthUrl { get; set; } = string.Empty;

    public string ApiUrl { get; set; } = string.Empty;

    public string BotToken { get; set; } = string.Empty;
}

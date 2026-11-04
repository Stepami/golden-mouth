namespace Reki.Infrastructure.Auth;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = "reki-api";

    public string ClientId { get; set; } = "reki-app";

    public string SigningKey { get; set; } = string.Empty;

    public string EncryptionKey { get; set; } = string.Empty;

    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(15);

    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(30);
}

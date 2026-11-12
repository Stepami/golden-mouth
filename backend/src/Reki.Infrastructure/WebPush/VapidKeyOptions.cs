namespace Reki.Infrastructure.WebPush;

internal sealed class VapidKeyOptions
{
    public const string SectionName = nameof(VapidKeyOptions);

    public string PublicKey { get; set; } = string.Empty;

    public string PrivateKey { get; set; } = string.Empty;
}
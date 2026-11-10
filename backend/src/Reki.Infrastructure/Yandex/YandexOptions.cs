using System.ComponentModel.DataAnnotations;

namespace Reki.Infrastructure.Yandex;

public sealed class YandexOptions
{
    public const string SectionName = "Yandex";

    [Required]
    public string ClientId { get; init; } = "799a21610c6b495c8d8c01fd9b8c4012";

    [Required]
    public string BaseUrl { get; init; } = "https://login.yandex.ru/";

    [Range(1, 60)]
    public int TimeoutSeconds { get; init; } = 10;
}

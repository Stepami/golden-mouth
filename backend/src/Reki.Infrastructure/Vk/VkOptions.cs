using System.ComponentModel.DataAnnotations;

namespace Reki.Infrastructure.Vk;

public sealed class VkOptions
{
    public const string SectionName = "Vk";

    [Range(1, long.MaxValue)]
    public long ApplicationId { get; init; }

    [Required]
    public string BaseUrl { get; init; } = "https://id.vk.ru/";

    [Range(1, 60)]
    public int TimeoutSeconds { get; init; } = 10;
}

using System.ComponentModel.DataAnnotations;

namespace Reki.Infrastructure.Persistence;

public sealed class MongoOptions
{
    public const string SectionName = "Mongo";

    [Required]
    public string ConnectionString { get; init; } = string.Empty;

    [Required]
    public string DatabaseName { get; init; } = string.Empty;
}

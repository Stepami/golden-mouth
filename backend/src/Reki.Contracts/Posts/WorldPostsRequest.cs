using System.ComponentModel.DataAnnotations;

namespace Reki.Contracts.Posts;

/// <summary>Query parameters for posts in an owned world.</summary>
public sealed record WorldPostsRequest : IValidatableObject
{
    [Required]
    public required string WorldId { get; init; }

    [Range(0, int.MaxValue)]
    public int PageNumber { get; init; }

    [Range(1, int.MaxValue - 1)]
    public int PageSize { get; init; } = 10;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if ((long)PageNumber * PageSize > int.MaxValue)
        {
            yield return new ValidationResult("Page offset exceeds the supported range.", [nameof(PageNumber)]);
        }
    }
}

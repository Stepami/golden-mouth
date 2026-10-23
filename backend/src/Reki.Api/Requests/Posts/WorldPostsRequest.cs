using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Reki.Api.Requests.Posts;

/// <summary>Query parameters for posts in an owned world.</summary>
public sealed record WorldPostsRequest : IValidatableObject
{
    private const int DefaultPageNumber = 0;
    private const int DefaultPageSize = 10;

    [Required]
    [BindRequired]
    [FromQuery(Name = "worldId")]
    public required string WorldId { get; init; }

    [DefaultValue(DefaultPageNumber)]
    [FromQuery(Name = "pageNumber")]
    [Range(0, int.MaxValue)]
    public int PageNumber { get; init; } = DefaultPageNumber;

    [DefaultValue(DefaultPageSize)]
    [FromQuery(Name = "pageSize")]
    [Range(1, int.MaxValue - 1)]
    public int PageSize { get; init; } = DefaultPageSize;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if ((long)PageNumber * PageSize > int.MaxValue)
        {
            yield return new ValidationResult("Page offset exceeds the supported range.", [nameof(PageNumber)]);
        }
    }
}

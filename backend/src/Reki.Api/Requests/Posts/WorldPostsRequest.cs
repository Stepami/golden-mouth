using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Reki.Api.Requests.Posts;

/// <summary>Query parameters for posts in an owned world.</summary>
public sealed record WorldPostsRequest : IValidatableObject
{
    private const int DefaultPageSize = 10;

    [Required]
    [BindRequired]
    [FromQuery(Name = "worldId")]
    [RegularExpression("^(?:[0-9a-fA-F]{32}|[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})$")]
    public required string WorldId { get; init; }

    [FromQuery(Name = "cursor")]
    public Guid? Cursor { get; init; }

    [DefaultValue(DefaultPageSize)]
    [FromQuery(Name = "pageSize")]
    [Range(1, int.MaxValue - 1)]
    public int PageSize { get; init; } = DefaultPageSize;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Cursor is { Version: not 7 })
        {
            yield return new ValidationResult("Cursor must be a version 7 UUID.", [nameof(Cursor)]);
        }
    }
}

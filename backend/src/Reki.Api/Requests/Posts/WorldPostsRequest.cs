using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Api.Requests.Posts;

/// <summary>Query parameters for posts in an owned world.</summary>
public sealed record WorldPostsRequest : IValidatableObject
{
    private const int DefaultPageSize = 10;

    [Required]
    [BindRequired]
    [FromQuery(Name = "worldId")]
    public required WorldId WorldId { get; init; }

    [FromQuery(Name = "cursor")]
    public PostId? Cursor { get; init; }

    [DefaultValue(DefaultPageSize)]
    [FromQuery(Name = "pageSize")]
    [Range(1, int.MaxValue - 1)]
    public int PageSize { get; init; } = DefaultPageSize;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Cursor is { Value.Version: not 7 })
        {
            yield return new ValidationResult("Cursor must be a version 7 UUID.", [nameof(Cursor)]);
        }
    }
}

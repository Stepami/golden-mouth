using System.ComponentModel.DataAnnotations;
using Reki.Domain.Posts;

namespace Reki.Api.Requests.Posts;

public sealed record CreatePostRequest
{
    [Required(AllowEmptyStrings = true)]
    [StringLength(2000)]
    public required string Text { get; init; }

    public PostId? RepostId { get; init; }

    public bool OwnWords { get; init; } = true;
}

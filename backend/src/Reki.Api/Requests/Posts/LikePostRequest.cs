using System.ComponentModel.DataAnnotations;
using Reki.Domain.Posts;

namespace Reki.Api.Requests.Posts;

public sealed record LikePostRequest
{
    [Required]
    public required AuthorId AuthorId { get; init; }
}

using Reki.Domain.Posts;

namespace Reki.Application.Posts.Responses.PostResult;

public sealed record RibbonResponse(string Icon, string Title, string? Sub)
{
    public static IReadOnlyList<RibbonResponse> Create(PostViralityStatus virality) =>
        virality switch
        {
            PostViralityStatus.Viral or PostViralityStatus.ViralCard =>
            [
                new RibbonResponse("🚀", "залёт", null),
            ],
            _ => [],
        };
}


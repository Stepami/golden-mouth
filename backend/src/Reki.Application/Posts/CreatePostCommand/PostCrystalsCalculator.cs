using Reki.Domain.Player;
using Reki.Domain.Posts;

namespace Reki.Application.Posts.CreatePostCommand;

internal sealed class PostCrystalsCalculator
{
    public int GetBonus(
        GamePost post,
        long postsCount,
        IReadOnlyDictionary<SkillId, bool> levelChange)
    {
        var crystals = 0;

        if (post is
            {
                Virality: PostViralityStatus.None,
                RepostId: null,
            }
            && postsCount % 5 == 0)
        {
            crystals++;
        }

        if (post.Virality is PostViralityStatus.Viral)
        {
            crystals++;
        }

        if (levelChange.Any(skill => skill.Value))
        {
            crystals++;
        }

        return crystals;
    }
}
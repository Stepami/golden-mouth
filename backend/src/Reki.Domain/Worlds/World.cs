using Reki.Domain.Player;
using Reki.Domain.Posts;

namespace Reki.Domain.Worlds;

public sealed record World
{
    public required WorldId Id { get; init; }

    public required WorldInterface Interface { get; init; }

    public required WorldPlayer Player { get; init; }

    public WorldId? TemplateId { get; init; }

    public WorldDifficulty Difficulty { get; init; } = WorldDifficulty.Normal;

    public bool IsArchived { get; init; }

    public IReadOnlyList<string> Roles { get; init; } = [];

    public IReadOnlyList<string> Voices { get; init; } = [];

    public IReadOnlyList<string> PostIdeas { get; init; } = [];

    public IReadOnlyList<string> DemoPosts { get; init; } = [];

    public IReadOnlyList<WorldRule> Rules { get; init; } = [];

    public IReadOnlyList<WorldStar> Stars { get; init; } = [];

    public int Version { get; init; }

    public World Instantiate(PlayerId owner)
    {
        if (Stars.Select(star => star.Id).Distinct().Count() != Stars.Count)
        {
            throw new ArgumentException("Star identifiers must be unique in a world.");
        }

        return this with
        {
            Id = WorldId.New(),
            Version = 0,
            Player = Player with
            {
                PlayerId = owner,
                Achievements = Player.Achievements.Select(item => item with { IsOpen = false }).ToArray(),
            },
        };
    }

    public WorldStar? FindStar(StarId starId) => Stars.SingleOrDefault(star => star.Id == starId);

    public WorldStar? FindStar(AuthorId authorId) => Stars.FirstOrDefault(star => star.Id.ToAuthorId() == authorId);

    public World PublishPost(
        long subscriberDelta,
        bool viral,
        IReadOnlyDictionary<SkillId,
        SkillProgress> skills)
    {
        var updated = this with
        {
            Version = Version + 1,
            Player = Player with
            {
                Subscribers = Math.Max(0, Player.Subscribers + subscriberDelta),
            },
        };
        if (viral)
        {
            updated = updated.UnlockAchievement(AchievementCatalog.FirstViral);
        }

        if (updated.Player.Subscribers >= 10_000)
        {
            updated = updated.UnlockAchievement(AchievementCatalog.TenThousand);
        }

        if (skills.Values.Any(skill => skill.Level >= 3))
        {
            updated = updated.UnlockAchievement(AchievementCatalog.SkillMastery);
        }

        return updated;
    }

    public World UnlockAchievement(string id) => this with
    {
        Player = Player with
        {
            Achievements = Player.Achievements.Select(item => item.Id == id ? item with { IsOpen = true } : item).ToArray(),
        },
    };

    public World AwardThreadBonus(long bonus) => this with
    {
        Version = Version + 1,
        Player = Player with
        {
            Subscribers = Math.Max(0, Player.Subscribers + bonus),
        },
    };
}

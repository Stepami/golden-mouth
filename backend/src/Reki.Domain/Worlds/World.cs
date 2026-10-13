using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Social;

namespace Reki.Domain.Worlds;

public sealed record World
{
    public required WorldId Id { get; init; }

    public required WorldInterface Interface { get; init; }

    public required WorldPlayer Player { get; init; }

    public required string BalanceKey { get; init; }

    public IReadOnlyList<string> Roles { get; init; } = [];

    public IReadOnlyList<string> Voices { get; init; } = [];

    public IReadOnlyList<string> PostIdeas { get; init; } = [];

    public IReadOnlyList<string> DemoPosts { get; init; } = [];

    public IReadOnlyList<WorldRule> Rules { get; init; } = [];

    public IReadOnlyList<WorldStar> Stars { get; init; } = [];

    public int Version { get; init; }

    public World Instantiate(PlayerId owner, DateTimeOffset periodStart)
    {
        if (Stars.Select(star => star.Id).Distinct().Count() != Stars.Count)
        {
            throw new ArgumentException("Star identifiers must be unique in a world.");
        }

        return this with
        {
            Id = WorldId.New(),
            Version = 0,
            Stars = Stars.Select(star => star with
            {
                Relation = 0,
                RelationGainedInPeriod = 0,
                RelationPeriodStartedAtUtc = periodStart,
            }).ToArray(),
            Player = Player with
            {
                PlayerId = owner,
                PeriodStartedAtUtc = periodStart,
                Wants = Player.Wants.Select(want => want with { Done = false }).ToArray(),
                Achievements = Player.Achievements.Select(item => item with { IsOpen = false }).ToArray(),
                LastPostAtUtc = null,
                RestedPostsRemaining = 0,
            },
        };
    }

    public WorldStar? FindStar(StarId starId) => Stars.SingleOrDefault(star => star.Id == starId);

    public WorldStar? FindStar(AuthorId authorId) => Stars.FirstOrDefault(star => star.Id.ToAuthorId() == authorId);

    public bool HasStar(StarId starId, string name) => Stars.Any(star => star.Id == starId && star.Name == name);

    public World StartNewPeriod(DateTimeOffset periodStart, int restedPosts) => this with
    {
        Stars = Stars.Select(star => star with { RelationGainedInPeriod = 0, RelationPeriodStartedAtUtc = periodStart }).ToArray(),
        Version = Version + 1,
        Player = Player with
        {
            PeriodStartedAtUtc = periodStart,
            RestedPostsRemaining = restedPosts,
            Wants = Player.Wants.Select(want => want with { Done = false }).ToArray(),
        },
    };

    public World PublishPost(
        long subscriberDelta,
        bool viral,
        IReadOnlyDictionary<SkillId,
        SkillProgress> skills,
        string? completedWantId,
        StarId starId,
        DateTimeOffset now,
        int relationMinimum,
        int relationMaximum,
        int relationMaxGainPerDay,
        int starCardAt)
    {
        var completedWant = Player.Wants.Any(want => want.Id == completedWantId && !want.Done);
        var stars = Stars.Select(star =>
        {
            if (star.Id != starId || !completedWant)
            {
                return star;
            }

            var gain = star.RelationGainedInPeriod < relationMaxGainPerDay ? 1 : 0;
            return star with
            {
                Relation = Math.Clamp(star.Relation + gain, relationMinimum, relationMaximum),
                RelationGainedInPeriod = Math.Min(relationMaxGainPerDay, star.RelationGainedInPeriod + gain),
            };
        }).ToArray();
        var updated = this with
        {
            Stars = stars,
            Version = Version + 1,
            Player = Player with
            {
                Subscribers = Math.Max(0, Player.Subscribers + subscriberDelta),
                Wants = Player.Wants.Select(want => want.Id == completedWantId ? want with { Done = true } : want).ToArray(),
                LastPostAtUtc = now,
                RestedPostsRemaining = Math.Max(0, Player.RestedPostsRemaining - 1),
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

        if (stars.Any(star => star.Id == starId && star.Relation >= starCardAt))
        {
            updated = updated.UnlockAchievement(AchievementCatalog.StarCard);
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

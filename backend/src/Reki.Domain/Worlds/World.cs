using Reki.Domain.Player;
using Reki.Domain.Posts;

namespace Reki.Domain.Worlds;

public sealed class World
{
    private readonly List<string> _roles;
    private readonly List<string> _voices;
    private readonly List<string> _postIdeas;
    private readonly List<string> _demoPosts;
    private readonly List<WorldRule> _rules;
    private readonly List<WorldStar> _stars;

    public World(
        WorldId id,
        WorldInterface @interface,
        WorldPlayer player,
        WorldId? templateId,
        WorldDifficulty difficulty,
        bool isArchived,
        IReadOnlyList<string> roles,
        IReadOnlyList<string> voices,
        IReadOnlyList<string> postIdeas,
        IReadOnlyList<string> demoPosts,
        IReadOnlyList<WorldRule> rules,
        IReadOnlyList<WorldStar> stars,
        int version)
    {
        Id = id;
        Interface = @interface;
        Player = new WorldPlayer(player.PlayerId, player.Voice, player.Drama, player.Name, player.Role, player.Avatar, player.Subscribers, player.Achievements);
        TemplateId = templateId;
        Difficulty = difficulty;
        IsArchived = isArchived;
        Version = version;
        _roles = [.. roles];
        _voices = [.. voices];
        _postIdeas = [.. postIdeas];
        _demoPosts = [.. demoPosts];
        _rules = [.. rules];
        _stars = [.. stars];
        Roles = _roles.AsReadOnly();
        Voices = _voices.AsReadOnly();
        PostIdeas = _postIdeas.AsReadOnly();
        DemoPosts = _demoPosts.AsReadOnly();
        Rules = _rules.AsReadOnly();
        Stars = _stars.AsReadOnly();
    }

    public WorldId Id { get; private set; }

    public WorldInterface Interface { get; private set; }

    public WorldPlayer Player { get; private set; }

    public WorldId? TemplateId { get; private set; }

    public WorldDifficulty Difficulty { get; private set; }

    public bool IsArchived { get; private set; }

    public IReadOnlyList<string> Roles { get; }

    public IReadOnlyList<string> Voices { get; }

    public IReadOnlyList<string> PostIdeas { get; }

    public IReadOnlyList<string> DemoPosts { get; }

    public IReadOnlyList<WorldRule> Rules { get; }

    public IReadOnlyList<WorldStar> Stars { get; }

    public int Version { get; private set; }

    public World Instantiate(PlayerId owner, long? startingSubscribers = null)
    {
        if (Stars.Select(star => star.Id).Distinct().Count() != Stars.Count)
        {
            throw new ArgumentException("Star identifiers must be unique in a world.");
        }

        var player = new WorldPlayer(owner, Player.Voice, Player.Drama, Player.Name, Player.Role, Player.Avatar, startingSubscribers ?? Player.Subscribers, Player.Achievements.Select(item => item with { IsOpen = false }).ToArray());
        return new World(WorldId.New(), Interface, player, TemplateId, Difficulty, IsArchived, Roles, Voices, PostIdeas, DemoPosts, Rules, Stars, 0);
    }

    public WorldStar? FindStar(AuthorId authorId) => Stars.FirstOrDefault(star => star.Id.ToAuthorId() == authorId);

    public World PublishPost(PostProgression progression)
    {
        var changed = Player.SetSubscribers(Math.Max(0, Player.Subscribers + progression.SubscriberDelta));
        if (progression.Viral)
        {
            changed |= Player.UnlockAchievement(AchievementCatalog.FirstViral);
        }

        if (Player.Subscribers >= 10_000)
        {
            changed |= Player.UnlockAchievement(AchievementCatalog.TenThousand);
        }

        if (progression.Skills.Values.Any(skill => skill.Level >= 3))
        {
            changed |= Player.UnlockAchievement(AchievementCatalog.SkillMastery);
        }

        if (changed)
        {
            Version++;
        }

        return this;
    }

    public World UnlockAchievement(string id)
    {
        if (Player.UnlockAchievement(id))
        {
            Version++;
        }

        return this;
    }

    public World AwardThreadBonus(long bonus)
    {
        if (Player.SetSubscribers(Math.Max(0, Player.Subscribers + bonus)))
        {
            Version++;
        }

        return this;
    }

    public void UpdateDefinition(World definition)
    {
        var changed = Interface != definition.Interface || Difficulty != definition.Difficulty ||
            !_roles.SequenceEqual(definition.Roles) || !_voices.SequenceEqual(definition.Voices) ||
            !_postIdeas.SequenceEqual(definition.PostIdeas) || !_demoPosts.SequenceEqual(definition.DemoPosts) ||
            !_rules.SequenceEqual(definition.Rules) || !_stars.SequenceEqual(definition.Stars);
        changed |= Player.UpdateDefinition(definition.Player);
        if (!changed)
        {
            return;
        }

        Interface = definition.Interface;
        Difficulty = definition.Difficulty;
        Replace(_roles, definition.Roles);
        Replace(_voices, definition.Voices);
        Replace(_postIdeas, definition.PostIdeas);
        Replace(_demoPosts, definition.DemoPosts);
        Replace(_rules, definition.Rules);
        Replace(_stars, definition.Stars);
        Version++;
    }

    private static void Replace<T>(List<T> target, IReadOnlyList<T> source)
    {
        if (!target.SequenceEqual(source))
        {
            target.Clear();
            target.AddRange(source);
        }
    }
}

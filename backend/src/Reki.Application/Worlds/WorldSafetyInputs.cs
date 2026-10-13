using Reki.Domain.Worlds;

namespace Reki.Application.Worlds;

internal static class WorldSafetyInputs
{
    public static IReadOnlyList<string> From(World world) =>
    [
        world.Interface.Title, world.Interface.Description, world.Interface.Where,
        world.Interface.Summary, world.Interface.FirstContext, world.Interface.FirstPrompt,
        world.Player.Name, world.Player.Role, world.Player.Voice,
        .. world.Roles, .. world.Voices, .. world.PostIdeas, .. world.DemoPosts,
        .. world.Rules.Select(rule => rule.Text),
        .. world.Stars.SelectMany(star => new[] { star.Name, star.Role, star.About, star.Voice }),
        .. world.Player.Wants.SelectMany(want => new[] { want.Text, want.Reward }),
        .. world.Player.Achievements.Select(item => item.Title),
    ];
}

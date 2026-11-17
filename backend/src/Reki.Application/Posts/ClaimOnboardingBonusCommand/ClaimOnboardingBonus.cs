using Mediator;
using Reki.Domain.Player;
using Reki.Domain.Posts;

namespace Reki.Application.Posts.ClaimOnboardingBonusCommand;

public sealed record ClaimOnboardingBonus(PlayerProfile Player, PostId PostId) : ICommand<bool>;

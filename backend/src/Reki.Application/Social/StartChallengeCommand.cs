using Mediator;
using Reki.Domain.Player;

namespace Reki.Application.Social;

public sealed record StartChallengeCommand(TelegramUserId TelegramUserId, PlayerId FriendId) : IRequest;


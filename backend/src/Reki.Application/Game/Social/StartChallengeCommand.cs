using Mediator;

namespace Reki.Application.Game.Social;

public sealed record StartChallengeCommand(long TelegramUserId, string FriendId) : IRequest;


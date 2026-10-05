using Mediator;

namespace Reki.Application.Social;

public sealed record StartChallengeCommand(long TelegramUserId, string FriendId) : IRequest;


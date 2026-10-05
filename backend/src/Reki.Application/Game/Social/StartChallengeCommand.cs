using Mediator;
using Reki.Domain.Game;

namespace Reki.Application.Game;

public sealed record StartChallengeCommand(long TelegramUserId, string FriendId) : IRequest;


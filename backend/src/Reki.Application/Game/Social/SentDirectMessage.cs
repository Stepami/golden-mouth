using Mediator;
using Reki.Domain.Game;

namespace Reki.Application.Game;

public sealed record SentDirectMessage(string Reply, int RepliesLeft);


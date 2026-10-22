using Mediator;
using Reki.Domain.Social;
using Reki.Domain.Worlds;

namespace Reki.Application.Social;

public sealed record SendDirectMessage(StarId StarId, string Text, WorldId WorldId)
    : IRequest<SentDirectMessage>;
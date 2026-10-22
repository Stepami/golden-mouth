using Mediator;
using Reki.Domain.Social;
using Reki.Domain.Worlds;

namespace Reki.Application.Social.SendDirectMessageCommand;

public sealed record SendDirectMessage(StarId StarId, string Text, WorldId WorldId)
    : ICommand<SentDirectMessage>;
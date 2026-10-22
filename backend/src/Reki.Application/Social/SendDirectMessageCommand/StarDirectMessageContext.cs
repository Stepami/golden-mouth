using Reki.Application.Common.AI;
using Reki.Domain.Social;

namespace Reki.Application.Social.SendDirectMessageCommand;

public sealed record StarDirectMessageContext(
    StarAgentProfile Agent,
    string Text,
    IReadOnlyList<DirectMessage> History);

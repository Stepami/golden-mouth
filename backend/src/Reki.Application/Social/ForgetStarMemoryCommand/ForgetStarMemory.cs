using Mediator;
using Reki.Domain.Social;
using Reki.Domain.Worlds;

namespace Reki.Application.Social.ForgetStarMemoryCommand;

public sealed record ForgetStarMemory(StarId StarId, string Fact, WorldId WorldId) : ICommand;
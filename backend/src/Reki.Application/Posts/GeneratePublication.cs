using Mediator;
using Reki.Contracts.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts;

public sealed record GeneratePublication(
    string Id,
    string Text,
    bool OwnWords,
    string? WantId,
    WorldId WorldId) : IStreamRequest<PublicationStatusLiveEvent>;
using Mediator;
using Reki.Contracts.Posts;

namespace Reki.Application.Posts;

public sealed record GeneratePublicationCommand(
    string Id,
    string Text,
    bool OwnWords,
    string? WantId) : IStreamRequest<PublicationStatusLiveEvent>;

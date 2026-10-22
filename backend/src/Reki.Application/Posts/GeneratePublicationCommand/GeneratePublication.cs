using Mediator;
using Reki.Application.Posts.Responses;
using Reki.Application.Posts.Responses.PublicationStatusLiveEvent;
using Reki.Domain.Worlds;
using PublicationStatusLiveEvent = Reki.Application.Posts.Responses.PublicationStatusLiveEvent.PublicationStatusLiveEvent;

namespace Reki.Application.Posts.GeneratePublicationCommand;

public sealed record GeneratePublication(
    string Id,
    string Text,
    bool OwnWords,
    string? WantId,
    WorldId WorldId) : IStreamCommand<PublicationStatusLiveEvent>;
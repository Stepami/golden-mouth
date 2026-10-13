using Reki.Application.Common.Exceptions;
using Reki.Contracts.Posts;
using Reki.Domain.Player;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts;

public sealed record Publication(
    string Id,
    PlayerId PlayerId,
    string Text,
    string RequestHash,
    bool OwnWords,
    string? WantId,
    PublicationStatus Status,
    IReadOnlyList<PublicationPart> Parts,
    PublishedPost? Result,
    DateTime? ExpiresAtUtc,
    WorldId WorldId)
{
    public PublicationFailure? Failure { get; init; }

    public GeneratedPostTurn AssembleTurn()
    {
        var comments = Parts
            .Select(part => part.Comment)
            .OfType<GeneratedPostTurn>()
            .ToArray();
        var reactions = Parts
            .Select(part => part.Reaction)
            .OfType<string>()
            .ToArray();
        if (comments.Length != 1 || reactions.Length > 1)
        {
            throw new AiServiceUnavailableException("The model returned an invalid action set.");
        }

        var comment = comments[0];
        return comment with { Reaction = reactions.FirstOrDefault() ?? "🔥" };
    }
}
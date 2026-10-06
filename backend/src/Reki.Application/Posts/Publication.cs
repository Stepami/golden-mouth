using Reki.Application.Common.Exceptions;
using Reki.Application.Common.Safety;
using Reki.Contracts.Posts;
using Reki.Domain.Player;

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
    DateTime? ExpiresAtUtc)
{
    public GeneratedPostTurn AssembleTurn()
    {
        GeneratedPostTurn[] comments = Parts
            .Select(part => part.Comment)
            .OfType<GeneratedPostTurn>()
            .ToArray();
        string[] reactions = Parts
            .Select(part => part.Reaction)
            .OfType<string>()
            .ToArray();
        if (comments.Length != 1 || reactions.Length > 1)
        {
            throw new AiServiceUnavailableException("The model returned an invalid action set.");
        }

        GeneratedPostTurn comment = comments[0];
        if (!ContentSafetyPolicy.IsOutputAllowed(comment.Comment) ||
            !ContentSafetyPolicy.IsOutputAllowed(comment.Hook))
        {
            throw new AiServiceUnavailableException("The model output did not pass the safety policy.");
        }

        return comment with { Reaction = reactions.FirstOrDefault() ?? "🔥" };
    }
}

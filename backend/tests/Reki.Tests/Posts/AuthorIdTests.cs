using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Social;

namespace Reki.Tests.Posts;

public sealed class AuthorIdTests
{
    [Test]
    [Arguments(AuthorKind.Player)]
    [Arguments(AuthorKind.Star)]
    public async Task From_ValidKind_PreservesCompositeIdentityAsync(AuthorKind kind)
    {
        // Arrange
        const string identifier = "shared-identity";

        // Act
        var author = AuthorId.From((kind, identifier));

        // Assert
        await Assert.That(author.Value.Item1).IsEqualTo(kind);
        await Assert.That(author.Value.Item2).IsEqualTo(identifier);
    }

    [Test]
    [Arguments(AuthorKind.Player, null)]
    [Arguments(AuthorKind.Player, "")]
    [Arguments(AuthorKind.Star, " \t ")]
    [Arguments((AuthorKind)99, "valid-id")]
    public async Task TryFrom_InvalidKindOrIdentifier_RejectsValueAsync(AuthorKind kind, string? identifier)
    {
        // Arrange
#pragma warning disable NX0001 // Null is intentional invalid input for the value object's runtime validation.
        var input = (kind, identifier!);
#pragma warning restore NX0001

        // Act
        var accepted = AuthorId.TryFrom(input, out _);

        // Assert
        await Assert.That(accepted).IsFalse();
    }

    [Test]
    public async Task ToAuthorId_EqualUnderlyingIds_KeepsKindsDistinctAsync()
    {
        // Arrange
        var player = PlayerId.From("same-id");
        var star = StarId.From("same-id");

        // Act
        var playerAuthor = player.ToAuthorId();
        var starAuthor = star.ToAuthorId();

        // Assert
        await Assert.That(playerAuthor.Value.Item1).IsEqualTo(AuthorKind.Player);
        await Assert.That(starAuthor.Value.Item1).IsEqualTo(AuthorKind.Star);
        await Assert.That(playerAuthor.Value.Item2).IsEqualTo("same-id");
        await Assert.That(starAuthor.Value.Item2).IsEqualTo("same-id");
        await Assert.That(playerAuthor == starAuthor).IsFalse();
    }
}

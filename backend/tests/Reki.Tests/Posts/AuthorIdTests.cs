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
        var author = AuthorId.Create(kind, identifier);

        // Assert
        await Assert.That(author.Kind).IsEqualTo(kind);
        await Assert.That(author.Id).IsEqualTo(identifier);
    }

    [Test]
    [Arguments(AuthorKind.Player, null)]
    [Arguments(AuthorKind.Player, "")]
    [Arguments(AuthorKind.Star, " \t ")]
    [Arguments((AuthorKind)99, "valid-id")]
    public async Task TryFrom_InvalidKindOrIdentifier_RejectsValueAsync(AuthorKind kind, string? identifier)
    {
        // Arrange
        var input = $"{kind}_{identifier}";

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
        await Assert.That(playerAuthor.Kind).IsEqualTo(AuthorKind.Player);
        await Assert.That(starAuthor.Kind).IsEqualTo(AuthorKind.Star);
        await Assert.That(playerAuthor.Id).IsEqualTo("same-id");
        await Assert.That(starAuthor.Id).IsEqualTo("same-id");
        await Assert.That(playerAuthor == starAuthor).IsFalse();
    }
}

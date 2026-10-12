using System.Globalization;
using System.Text.Json;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Social;

namespace Reki.Tests.Posts;

public sealed class AuthorStringTests
{
    [Test]
    [Arguments("Player_x", AuthorKind.Player, "x")]
    [Arguments("Star_мир", AuthorKind.Star, "мир")]
    [Arguments("Player_a_Star_b", AuthorKind.Player, "a_Star_b")]
    [Arguments("Star_ Player_a ", AuthorKind.Star, " Player_a ")]
    [Arguments("Player_first last", AuthorKind.Player, "first last")]
    public async Task From_CanonicalString_PreservesExactIdentityAsync(string value, AuthorKind kind, string id)
    {
        // Arrange
        var expected = value;

        // Act
        var author = AuthorId.From(value);
        var created = AuthorId.Create(kind, id);

        // Assert
        await Assert.That(author.Value).IsEqualTo(expected);
        await Assert.That(author.Kind).IsEqualTo(kind);
        await Assert.That(author.Id).IsEqualTo(id);
        await Assert.That(created).IsEqualTo(author);
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments(" \t ")]
    [Arguments("Player")]
    [Arguments("Player_")]
    [Arguments("Star_")]
    [Arguments("Player_ \t ")]
    [Arguments("player_id")]
    [Arguments("star_id")]
    [Arguments("pLayer_id")]
    [Arguments("0_id")]
    [Arguments("1_id")]
    [Arguments("Other_id")]
    [Arguments("_id")]
    public async Task From_InvalidString_RejectsValueAsync(string? value)
    {
        // Arrange
#pragma warning disable NX0001 // Null deliberately exercises the generated public validation boundary.
        var input = value!;
#pragma warning restore NX0001

        // Act
        var accepted = AuthorId.TryFrom(input, out _);
        var create = () => AuthorId.From(input);

        // Assert
        await Assert.That(accepted).IsFalse();
        await Assert.That(create).ThrowsException();
    }

    [Test]
    [Arguments(AuthorKind.Player, null)]
    [Arguments(AuthorKind.Star, "")]
    [Arguments(AuthorKind.Player, " \t ")]
    [Arguments((AuthorKind)(-1), "id")]
    [Arguments((AuthorKind)2, "id")]
    public async Task Create_InvalidKindOrId_RejectsValueAsync(AuthorKind kind, string? id)
    {
        // Arrange
#pragma warning disable NX0001 // Null deliberately exercises the factory's validation boundary.
        var input = id!;
#pragma warning restore NX0001

        // Act
        var create = () => AuthorId.Create(kind, input);

        // Assert
        await Assert.That(create).ThrowsException();
    }

    [Test]
    [Arguments("Player_ I_id ")]
    [Arguments("Star_звезда_Star_id")]
    public async Task Parse_ValidStringWithDifferentProviders_PreservesValueAsync(string value)
    {
        // Arrange
        IFormatProvider[] providers = [CultureInfo.InvariantCulture, CultureInfo.GetCultureInfo("tr-TR")];

        // Act
        foreach (var provider in providers)
        {
            var parsed = Parse<AuthorId>(value, provider);
            var accepted = TryParse<AuthorId>(value, provider, out var tried);

            // Assert
            await Assert.That(parsed.Value).IsEqualTo(value);
            await Assert.That(accepted).IsTrue();
            await Assert.That(tried).IsEqualTo(parsed);
        }
    }

    [Test]
    [Arguments(null)]
    [Arguments("Star_")]
    [Arguments("star_id")]
    [Arguments("Player_ \t ")]
    public async Task TryParse_InvalidString_ReturnsFalseAsync(string? value)
    {
        // Arrange
        var provider = CultureInfo.GetCultureInfo("tr-TR");

        // Act
        var accepted = TryParse<AuthorId>(value, provider, out _);
#pragma warning disable NX0001 // Null deliberately exercises IParsable's failure behavior.
        var input = value!;
#pragma warning restore NX0001
        var parse = () => Parse<AuthorId>(input, provider);

        // Assert
        await Assert.That(accepted).IsFalse();
        await Assert.That(parse).ThrowsException();
    }

    [Test]
    public async Task ToAuthorId_SourceValues_PreservesNormalizedSourceIdentityAsync()
    {
        // Arrange
        var player = PlayerId.From("Mixed_Player");
        var star = StarId.From(" Mixed_Star ");

        // Act
        var playerAuthor = player.ToAuthorId();
        var starAuthor = star.ToAuthorId();

        // Assert
        await Assert.That(playerAuthor.Value).IsEqualTo("Player_Mixed_Player");
        await Assert.That(playerAuthor.Id).IsEqualTo(player.Value);
        await Assert.That(star.Value).IsEqualTo("mixed_star");
        await Assert.That(starAuthor.Value).IsEqualTo("Star_mixed_star");
        await Assert.That(starAuthor.Id).IsEqualTo(star.Value);
    }

    [Test]
    [Arguments("Player_ a_Star_b ")]
    [Arguments("Star_звезда")]
    public async Task Json_ValidAuthor_RoundTripsAsExactStringAsync(string value)
    {
        // Arrange
        var author = AuthorId.From(value);

        // Act
        var json = JsonSerializer.Serialize(author, AuthorTestJsonContext.Default.AuthorId);
        using var document = JsonDocument.Parse(json);
        var restored = JsonSerializer.Deserialize(json, AuthorTestJsonContext.Default.AuthorId);

        // Assert
        await Assert.That(JsonSerializer.IsReflectionEnabledByDefault).IsFalse();
        await Assert.That(document.RootElement.ValueKind).IsEqualTo(JsonValueKind.String);
        await Assert.That(document.RootElement.GetString()).IsEqualTo(value);
        await Assert.That(restored).IsEqualTo(author);
    }

    [Test]
    [Arguments("\"player_id\"")]
    [Arguments("\"Star_\"")]
    [Arguments("42")]
    [Arguments("{}")]
    [Arguments("null")]
    public async Task Json_InvalidAuthor_RejectsValueAsync(string json)
    {
        // Arrange
        var context = AuthorTestJsonContext.Default.AuthorId;

        // Act
        var deserialize = () => JsonSerializer.Deserialize(json, context);

        // Assert
        await Assert.That(deserialize).ThrowsException();
    }

    private static T Parse<T>(string value, IFormatProvider provider)
        where T : IParsable<T> => T.Parse(value, provider);

    private static bool TryParse<T>(string? value, IFormatProvider provider, out T result)
        where T : struct, IParsable<T> => T.TryParse(value, provider, out result);
}

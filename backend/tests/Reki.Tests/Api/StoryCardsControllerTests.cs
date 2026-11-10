using System.Buffers.Binary;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Reki.Tests.Api;

[ClassDataSource<ApiFactory>(Shared = SharedType.PerTestSession)]
[ParallelGroup("Api", Order = 1)]
[NotInParallel("ApiState")]
public sealed class StoryCardsControllerTests(ApiFactory factory) : GameApiTestBase(factory)
{
    [Test]
    public async Task PublicStoryCard_WithoutAuthentication_RequiresExistingOwnedPostAsync()
    {
        // Arrange
        const long ownerTelegramId = 1202;
        const long otherTelegramId = 1203;
        using var ownerApi = Factory.CreateApiClient(ownerTelegramId);
        using var otherApi = Factory.CreateApiClient(otherTelegramId);
        await CreatePlayerWithWorldAsync(ownerApi);
        await CreatePlayerWithWorldAsync(otherApi);
        await PublishAsync(ownerTelegramId, "публичная карточка");

        var owner = await Database.GetCollection<BsonDocument>("players")
            .Find(new BsonDocument("ExternalUserIds", $"telegram:{ownerTelegramId}"))
            .SingleAsync();
        var other = await Database.GetCollection<BsonDocument>("players")
            .Find(new BsonDocument("ExternalUserIds", $"telegram:{otherTelegramId}"))
            .SingleAsync();
        var ownerId = owner["_id"].AsBsonBinaryData.ToGuid().ToString("N");
        var otherId = other["_id"].AsBsonBinaryData.ToGuid().ToString("N");
        var post = await Database.GetCollection<BsonDocument>("posts")
            .Find(new BsonDocument("AuthorId", $"Player_{ownerId}"))
            .SingleAsync();
        var postId = post["_id"].AsBsonBinaryData.ToGuid().ToString("N");
        using var anonymous = Factory.CreateApiClient();

        // Act
        using var response = await anonymous.StoryCards.GetPublicStoryCardAsync(ownerId, postId, CancellationToken.None);
        using var output = new MemoryStream();
        await response.Stream.CopyToAsync(output, CancellationToken.None);
        var png = output.ToArray();
        var wrongOwner = await Assert.ThrowsAsync<StoryCardsApiException>(() => anonymous.StoryCards.GetPublicStoryCardAsync(
            otherId, postId, CancellationToken.None));
        await Assert.That(wrongOwner).IsNotNull();
        var invalidId = await Assert.ThrowsAsync<StoryCardsApiException>(() => anonymous.StoryCards.GetPublicStoryCardAsync(
            "invalid", postId, CancellationToken.None));
        await Assert.That(invalidId).IsNotNull();
        await ownerApi.Player.DeleteCurrentPlayerAsync(CancellationToken.None);
        var deleted = await Assert.ThrowsAsync<StoryCardsApiException>(() => anonymous.StoryCards.GetPublicStoryCardAsync(
            ownerId, postId, CancellationToken.None));
        await Assert.That(deleted).IsNotNull();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(StatusCodes.Status200OK);
        await Assert.That(response.Headers["Content-Type"].Single()).IsEqualTo("image/png");
        await Assert.That(png.AsSpan(0, 8).ToArray())
            .IsEquivalentTo(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        await Assert.That(BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16, 4))).IsEqualTo(1080);
        await Assert.That(BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20, 4))).IsEqualTo(1920);
        await Assert.That(wrongOwner.StatusCode).IsEqualTo(StatusCodes.Status404NotFound);
        await Assert.That(invalidId.StatusCode).IsEqualTo(StatusCodes.Status400BadRequest);
        await Assert.That(deleted.StatusCode).IsEqualTo(StatusCodes.Status404NotFound);
    }
}

using System.Buffers.Binary;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Reki.Tests.Api;

[ClassDataSource<ApiFactory>]
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
        await OnboardAsync(ownerApi);
        await OnboardAsync(otherApi);
        await PublishAsync(ownerTelegramId, "публичная карточка");

        var owner = await Database.GetCollection<BsonDocument>("players")
            .Find(new BsonDocument("TelegramUserId", ownerTelegramId))
            .SingleAsync();
        var other = await Database.GetCollection<BsonDocument>("players")
            .Find(new BsonDocument("TelegramUserId", otherTelegramId))
            .SingleAsync();
        var ownerId = owner["_id"].AsString;
        var otherId = other["_id"].AsString;
        var post = await Database.GetCollection<BsonDocument>("posts")
            .Find(new BsonDocument("PlayerId", ownerId))
            .SingleAsync();
        var postId = post["_id"].AsString;
        using var anonymous = Factory.CreateClient();
        var publicPath = $"/cards/story/{ownerId}/{postId}.png";

        // Act
        using var response = await anonymous.GetAsync(publicPath);
        var png = await response.Content.ReadAsByteArrayAsync();
        using var wrongOwner = await anonymous.GetAsync(
            $"/cards/story/{otherId}/{postId}.png");
        using var invalidId = await anonymous.GetAsync(
            $"/cards/story/invalid/{postId}.png");
        await ownerApi.Player.DeleteCurrentPlayerAsync(CancellationToken.None);
        using var deleted = await anonymous.GetAsync(publicPath);

        // Assert
        await Assert.That((int)response.StatusCode).IsEqualTo(StatusCodes.Status200OK);
        await Assert.That(response.Content.Headers.ContentType?.MediaType).IsEqualTo("image/png");
        await Assert.That(png.AsSpan(0, 8).ToArray())
            .IsEquivalentTo(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        await Assert.That(BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16, 4))).IsEqualTo(1080);
        await Assert.That(BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20, 4))).IsEqualTo(1920);
        await Assert.That((int)wrongOwner.StatusCode).IsEqualTo(StatusCodes.Status404NotFound);
        await Assert.That((int)invalidId.StatusCode).IsEqualTo(StatusCodes.Status404NotFound);
        await Assert.That((int)deleted.StatusCode).IsEqualTo(StatusCodes.Status404NotFound);
    }
}

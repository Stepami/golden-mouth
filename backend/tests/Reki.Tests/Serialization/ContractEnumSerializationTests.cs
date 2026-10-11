using System.Text.Json;
using Reki.Api.Serialization;
using Reki.Contracts.Events;
using Reki.Contracts.Player;
using Reki.Contracts.Posts;
using Reki.Contracts.Social;
using Reki.Contracts.Telemetry;

namespace Reki.Tests.Serialization;

public sealed class ContractEnumSerializationTests
{
    [Test]
    [Arguments(SkillId.Brave, "\"brave\"")]
    [Arguments(Address.Nick, "\"nick\"")]
    [Arguments(Drama.Soft, "\"soft\"")]
    [Arguments(BirthGroup.Teen, "\"14-17\"")]
    [Arguments(BirthGroup.Adult, "\"18\\u002B\"")]
    [Arguments(FeedItemKind.News, "\"news\"")]
    [Arguments(DirectMessageAuthor.Star, "\"star\"")]
    [Arguments(GameEventKind.Collab, "\"collab\"")]
    [Arguments(LiveEventType.Views, "\"views\"")]
    [Arguments(ClientLogLevel.Info, "\"info\"")]
    [Arguments(PublicationStatus.ReadyForModeration, "\"ready_for_moderation\"")]
    public async Task Serialize_ContractEnum_PreservesJsonNameAsync(Enum value, string expectedJson)
    {
        // Arrange
        var context = ApiJsonSerializerContext.Default;

        // Act
        var json = JsonSerializer.Serialize(value, value.GetType(), context);
        var roundTrip = JsonSerializer.Deserialize(json, value.GetType(), context);

        // Assert
        await Assert.That(json).IsEqualTo(expectedJson);
        await Assert.That(roundTrip).IsEqualTo(value);
    }

    [Test]
    [Arguments(PublicationStatus.ReadyForModeration, "\"READYFORMODERATION\"")]
    [Arguments(PublicationStatus.ReadyForModeration, "1")]
    [Arguments(BirthGroup.Adult, "1")]
    [Arguments((PublicationStatus)42, "42")]
    public async Task Deserialize_ExistingInputForms_PreservesEnumValueAsync(Enum expectedValue, string json)
    {
        // Arrange
        var context = ApiJsonSerializerContext.Default;

        // Act
        var value = JsonSerializer.Deserialize(json, expectedValue.GetType(), context);

        // Assert
        await Assert.That(value).IsEqualTo(expectedValue);
    }
}

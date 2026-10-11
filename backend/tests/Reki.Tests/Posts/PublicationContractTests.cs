using System.Text.Json;
using Reki.Api.Serialization;
using Reki.Contracts.Posts;

namespace Reki.Tests.Posts;

public sealed class PublicationContractTests
{
    [Test]
    public async Task LiveEvent_BaseTypeSerializesDerivedPayloadAsync()
    {
        var json = JsonSerializer.Serialize(
            (LiveEvent)new ViewsLiveEvent(48),
            ApiJsonSerializerContext.Default.LiveEvent);
        using var document = JsonDocument.Parse(json);

        await Assert.That(document.RootElement.GetProperty("type").GetString()).IsEqualTo("views");
        await Assert.That(document.RootElement.GetProperty("views").GetInt64()).IsEqualTo(48);
    }

    [Test]
    public async Task PublicationStatus_DefaultContext_UsesSnakeCaseAsync()
    {
        var json = JsonSerializer.Serialize(
            PublicationStatus.ReadyForModeration,
            ApiJsonSerializerContext.Default.PublicationStatus);

        await Assert.That(json).IsEqualTo("\"ready_for_moderation\"");
    }
}

using System.Text.Json;
using Reki.Api.Serialization;
using Reki.Contracts.Posts;

namespace Reki.Tests.Posts;

public sealed class PublicationContractTests
{
    [Test]
    public async Task LiveEvent_UsesLowercaseTypeInActualSseSerializerAsync()
    {
        string json = JsonSerializer.Serialize(
            new ViewsLiveEvent(LiveEventType.Views, 48),
            ApiJsonSerializerContext.Default.ViewsLiveEvent);
        using JsonDocument document = JsonDocument.Parse(json);

        await Assert.That(document.RootElement.GetProperty("type").GetString()).IsEqualTo("views");
    }
}

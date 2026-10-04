using System.Text.Json;
using Reki.Tests.Serialization;

namespace Reki.Tests.Generated;

public abstract class RekiApiClientBase
{
    protected static void UpdateJsonSerializerSettings(JsonSerializerOptions settings)
    {
        settings.PropertyNameCaseInsensitive = true;
        settings.TypeInfoResolverChain.Insert(0, GeneratedClientJsonContext.Default);
    }
}

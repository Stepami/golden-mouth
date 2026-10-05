using System.Text.Json;
using Reki.Api.Serialization;

namespace Reki.Tests.Generated;

public abstract class RekiApiClientBase
{
    protected static void UpdateJsonSerializerSettings(JsonSerializerOptions settings)
    {
        settings.PropertyNameCaseInsensitive = true;
        settings.TypeInfoResolverChain.Insert(0, ApiJsonSerializerContext.Default);
    }
}

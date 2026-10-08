using System.Text.Json;
using Reki.Api.Serialization;

namespace Reki.Tests.Generated;

public abstract class RekiApiClientBase
{
    protected static void UpdateJsonSerializerSettings(JsonSerializerOptions settings)
    {
        settings.PropertyNameCaseInsensitive = true;
        settings.Converters.Insert(0, new ContractEnumConverterFactory());
        settings.TypeInfoResolverChain.Insert(0, ApiJsonSerializerContext.Default);
    }
}

using System.Text.Json;
using Reki.Infrastructure.Serialization;

namespace Reki.Tests.Generated;

public abstract class RekiApiClientBase
{
    protected static void UpdateJsonSerializerSettings(JsonSerializerOptions settings)
    {
        settings.PropertyNameCaseInsensitive = true;
        foreach (var converter in ApiJsonSerializerContext.Default.Options.Converters)
        {
            settings.Converters.Insert(0, converter);
        }

        settings.TypeInfoResolverChain.Insert(0, ApiJsonSerializerContext.Default);
    }
}
using System.Text.Json;
using System.Text.Json.Serialization;
using Reki.Contracts.Events;
using Reki.Contracts.Player;
using Reki.Contracts.Posts;
using Reki.Contracts.Social;
using Reki.Contracts.Telemetry;

namespace Reki.Api.Serialization;

public sealed class ContractEnumConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert == typeof(SkillId) ||
        typeToConvert == typeof(WorldId) ||
        typeToConvert == typeof(Address) ||
        typeToConvert == typeof(Drama) ||
        typeToConvert == typeof(BirthGroup) ||
        typeToConvert == typeof(FeedItemKind) ||
        typeToConvert == typeof(DirectMessageAuthor) ||
        typeToConvert == typeof(GameEventKind) ||
        typeToConvert == typeof(LiveEventType) ||
        typeToConvert == typeof(ClientLogLevel) ||
        typeToConvert == typeof(PublicationStatus);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        if (typeToConvert == typeof(PublicationStatus))
        {
            return Create<PublicationStatus>(JsonNamingPolicy.SnakeCaseLower, options);
        }

        if (typeToConvert == typeof(SkillId))
        {
            return Create<SkillId>(JsonNamingPolicy.CamelCase, options);
        }

        if (typeToConvert == typeof(WorldId))
        {
            return Create<WorldId>(JsonNamingPolicy.CamelCase, options);
        }

        if (typeToConvert == typeof(Address))
        {
            return Create<Address>(JsonNamingPolicy.CamelCase, options);
        }

        if (typeToConvert == typeof(Drama))
        {
            return Create<Drama>(JsonNamingPolicy.CamelCase, options);
        }

        if (typeToConvert == typeof(BirthGroup))
        {
            return Create<BirthGroup>(JsonNamingPolicy.CamelCase, options);
        }

        if (typeToConvert == typeof(FeedItemKind))
        {
            return Create<FeedItemKind>(JsonNamingPolicy.CamelCase, options);
        }

        if (typeToConvert == typeof(DirectMessageAuthor))
        {
            return Create<DirectMessageAuthor>(JsonNamingPolicy.CamelCase, options);
        }

        if (typeToConvert == typeof(GameEventKind))
        {
            return Create<GameEventKind>(JsonNamingPolicy.CamelCase, options);
        }

        if (typeToConvert == typeof(LiveEventType))
        {
            return Create<LiveEventType>(JsonNamingPolicy.CamelCase, options);
        }

        if (typeToConvert == typeof(ClientLogLevel))
        {
            return Create<ClientLogLevel>(JsonNamingPolicy.CamelCase, options);
        }

        throw new ArgumentOutOfRangeException(nameof(typeToConvert));
    }

    private static JsonConverter Create<TEnum>(JsonNamingPolicy namingPolicy, JsonSerializerOptions options)
        where TEnum : struct, Enum =>
        new JsonStringEnumConverter<TEnum>(namingPolicy).CreateConverter(typeof(TEnum), options);
}

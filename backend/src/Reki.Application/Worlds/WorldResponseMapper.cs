using Reki.Application.Worlds.Responses;
using Reki.Domain.Player;
using Reki.Domain.Worlds;
using Riok.Mapperly.Abstractions;

namespace Reki.Application.Worlds;

[Mapper]
public static partial class WorldResponseMapper
{
    [MapNestedProperties(nameof(World.Interface))]
    [MapNestedProperties(nameof(World.Player))]
    [MapperIgnoreSource(nameof(World.Version))]
    [MapperIgnoreSource(nameof(World.TemplateId))]
    [MapperIgnoreSource(nameof(World.IsArchived))]
    public static partial WorldResponse ToResponse(this World source);

    [MapperIgnoreSource(nameof(WorldStar.RelationGainedInPeriod))]
    [MapperIgnoreSource(nameof(WorldStar.RelationPeriodStartedAtUtc))]
    [MapperIgnoreTarget(nameof(WorldStarResponse.ChannelSubscribers))]
    private static partial WorldStarResponse MapStar(WorldStar source);

    private static partial WorldRuleResponse MapRule(WorldRule source);

    private static partial WantResponse MapWant(DailyWant source);

    [MapProperty(nameof(AchievementProgress.IsOpen), nameof(CollectionCardResponse.Open))]
    private static partial CollectionCardResponse MapAchievement(AchievementProgress source);
}

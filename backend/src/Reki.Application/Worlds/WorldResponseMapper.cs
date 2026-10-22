using Reki.Application.Worlds.Responses;
using Reki.Domain.Player;
using Reki.Domain.Worlds;
using Riok.Mapperly.Abstractions;

namespace Reki.Application.Worlds;

[Mapper]
public static partial class WorldResponseMapper
{
    [MapProperty("Id.Value", nameof(WorldResponse.Id))]
    [MapNestedProperties(nameof(World.Interface))]
    [MapNestedProperties(nameof(World.Player))]
    [MapperIgnoreSource(nameof(World.Version))]
    public static partial WorldResponse ToResponse(this World source);

    [MapProperty("Id.Value", nameof(WorldStarResponse.Id))]
    [MapperIgnoreSource(nameof(WorldStar.RelationGainedInPeriod))]
    [MapperIgnoreSource(nameof(WorldStar.RelationPeriodStartedAtUtc))]
    private static partial WorldStarResponse MapStar(WorldStar source);

    private static partial WorldRuleResponse MapRule(WorldRule source);

    private static partial WantResponse MapWant(DailyWant source);

    [MapProperty(nameof(AchievementProgress.IsOpen), nameof(CollectionCardResponse.Open))]
    private static partial CollectionCardResponse MapAchievement(AchievementProgress source);
}

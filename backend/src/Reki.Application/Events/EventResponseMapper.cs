using Reki.Application.Events.Responses;
using Reki.Domain.Events;
using Riok.Mapperly.Abstractions;

namespace Reki.Application.Events;

[Mapper(EnumMappingIgnoreCase = true)]
public static partial class EventResponseMapper
{
    [MapperIgnoreSource(nameof(DailyGameEvent.PlayerId))]
    [MapperIgnoreSource(nameof(DailyGameEvent.DayStartedAtUtc))]
    [MapperIgnoreSource(nameof(DailyGameEvent.Answer))]
    [MapperIgnoreSource(nameof(DailyGameEvent.WorldId))]
    public static partial GameEventResponse ToResponse(this DailyGameEvent source);
}
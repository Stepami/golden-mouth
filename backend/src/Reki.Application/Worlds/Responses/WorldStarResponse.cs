using Reki.Domain.Worlds;

namespace Reki.Application.Worlds.Responses;

public sealed record WorldStarResponse(
    StarId Id,
    string Name,
    string Role,
    int Trait,
    string About,
    string Voice,
    string AvatarSeed,
    int Relation,
    long? ChannelSubscribers = null);

using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Reki.Contracts.Game;

public sealed record ChallengeResponse(
    string Id,
    string Me,
    FriendResponse Rival,
    int Day,
    int Days,
    int MyScore,
    int RivalScore,
    string BestPost);


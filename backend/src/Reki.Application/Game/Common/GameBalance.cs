namespace Reki.Application.Game;

public sealed record GameBalance(
    int FreePostsPerDay,
    int PlusPostsPerDay,
    TimeOnly PostsResetTimeMoscow,
    int PlusInterestBonus,
    long StartingSubscribers,
    int DirectMessageRepliesPerDay);

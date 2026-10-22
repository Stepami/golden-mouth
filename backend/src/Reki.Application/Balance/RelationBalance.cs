namespace Reki.Application.Balance;

public sealed record RelationBalance(
    int Minimum,
    int Maximum,
    int MaxGainPerDay,
    int StarCardAt,
    int DirectMessageRepliesPerDay);

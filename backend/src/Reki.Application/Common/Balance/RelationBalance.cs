namespace Reki.Application.Common.Balance;

public sealed record RelationBalance(
    int Minimum,
    int Maximum,
    int MaxGainPerDay,
    int StarCardAt,
    int DirectMessageRepliesPerDay);

namespace Reki.Application.Posts.AI.Safety;

public sealed class CrisisDetectedException(bool isTeen) : Exception("A crisis signal was detected.")
{
    public bool IsTeen { get; } = isTeen;
}
namespace Reki.Application.Posts;

public sealed class CrisisDetectedException(bool isTeen) : Exception("A crisis signal was detected.")
{
    public bool IsTeen { get; } = isTeen;
}
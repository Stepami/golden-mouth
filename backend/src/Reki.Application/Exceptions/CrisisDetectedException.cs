namespace Reki.Application.Exceptions;

public sealed class CrisisDetectedException(bool isTeen) : Exception("A crisis signal was detected.")
{
    public bool IsTeen { get; } = isTeen;
}
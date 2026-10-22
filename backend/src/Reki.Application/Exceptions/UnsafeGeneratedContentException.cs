namespace Reki.Application.Exceptions;

public sealed class UnsafeGeneratedContentException(string category) : Exception("The generated content failed safety review.")
{
    public string Category { get; } = category;
}

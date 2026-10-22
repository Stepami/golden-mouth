namespace Reki.Application.Posts.AI.Safety;

public sealed class UnsafeGeneratedContentException(string category) : Exception("The generated content failed safety review.")
{
    public string Category { get; } = category;
}

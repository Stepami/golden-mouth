namespace Reki.Application.Exceptions;

public sealed class ContentRejectedException : Exception
{
    public ContentRejectedException(string category = "uncertain")
        : base("The content did not pass the 14+ safety policy.")
    {
        Category = category;
    }

    public string Category { get; }
}
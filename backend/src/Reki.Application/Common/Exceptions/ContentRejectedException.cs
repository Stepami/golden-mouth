namespace Reki.Application.Common.Exceptions;

public sealed class ContentRejectedException : Exception
{
    public ContentRejectedException()
        : base("The content did not pass the 14+ safety policy.")
    {
    }
}
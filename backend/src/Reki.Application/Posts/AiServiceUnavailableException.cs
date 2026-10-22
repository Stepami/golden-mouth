namespace Reki.Application.Posts;

public sealed class AiServiceUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);
namespace Reki.Application.Posts.AI;

public sealed class AiServiceUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);
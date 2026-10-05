namespace Reki.Application.Common.Exceptions;

public sealed class AiServiceUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);
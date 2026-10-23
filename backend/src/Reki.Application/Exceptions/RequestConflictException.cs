namespace Reki.Application.Exceptions;

public sealed class RequestConflictException(string message) : Exception(message);

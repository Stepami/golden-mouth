namespace Reki.Application.Common.Exceptions;

public sealed class RequestConflictException(string message) : Exception(message);

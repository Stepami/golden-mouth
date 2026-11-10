namespace Reki.Application.Exceptions;

public sealed class UnitOfWorkException(
    string message,
    Exception innerException) :
    Exception(message, innerException);
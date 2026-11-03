using System.Diagnostics.CodeAnalysis;

namespace Reki.Application.Auth;

public interface IExternalUserIdProvider
{
    bool TryGetExternalUserId([NotNullWhen(true)] out long? externalUserId);
}
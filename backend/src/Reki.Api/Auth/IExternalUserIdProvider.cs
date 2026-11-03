using System.Diagnostics.CodeAnalysis;

namespace Reki.Api.Auth;

public interface IExternalUserIdProvider
{
    bool TryGetExternalUserId([NotNullWhen(true)] out long? externalUserId);
}
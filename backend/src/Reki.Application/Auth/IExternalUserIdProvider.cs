using System.Diagnostics.CodeAnalysis;
using Reki.Domain.Player;

namespace Reki.Application.Auth;

public interface IExternalUserIdProvider
{
    bool TryGetExternalUserId([NotNullWhen(true)] out ExternalUserId? externalUserId);
}
using Reki.Domain.Player;

namespace Reki.Application.Auth;

public interface IExternalUserIdProvider
{
    Task<ExternalUserId?> GetExternalUserIdOrNullAsync(CancellationToken cancellationToken);
}

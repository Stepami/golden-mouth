using System.Globalization;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using OpenIddict.Abstractions;
using Reki.Application.Auth;
using Reki.Application.Player;
using Reki.Application.Telemetry;
using Reki.Domain.Player;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Reki.Infrastructure.Auth;

internal sealed class AuthSessionService(
    IOpenIddictApplicationManager applications,
    IOpenIddictAuthorizationManager authorizations,
    IOpenIddictTokenManager tokens,
    IOptions<AuthOptions> options,
    IPlayerRepository players,
    ProductTelemetry telemetry) : IAuthSessionService
{
    public async Task<bool> LogoutAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        var token = await tokens.FindByReferenceIdAsync(refreshToken, cancellationToken);
        if (token is null || !await tokens.HasTypeAsync(token, TokenTypeIdentifiers.RefreshToken, cancellationToken))
        {
            return false;
        }

        var authorizationId = await tokens.GetAuthorizationIdAsync(token, cancellationToken);
        if (authorizationId is not null &&
            await authorizations.FindByIdAsync(authorizationId, cancellationToken) is { } authorization)
        {
            await authorizations.TryRevokeAsync(authorization, cancellationToken);
        }

        await tokens.TryRevokeAsync(token, cancellationToken);
        return true;
    }

    public async Task<ClaimsPrincipal?> IssueAsync(
        IExternalUserIdProvider externalUserIdProvider,
        string clientId,
        CancellationToken cancellationToken)
    {
        var application = await applications.FindByClientIdAsync(clientId, cancellationToken);
        if (application is null)
        {
            return null;
        }

        if (!externalUserIdProvider.TryGetExternalUserId(out var telegramId))
        {
            return null;
        }

        var externalId = ExternalUserId.From($"telegram:{telegramId.Value.ToString(CultureInfo.InvariantCulture)}");
        var player = await players.GetPlayerAsync(externalId, cancellationToken);
        if (player is null)
        {
            player = new PlayerProfile
            {
                Id = PlayerId.New(),
                ExternalUserIds = [externalId],
                BirthGroup = string.Empty,
                Address = Address.Nick,
                Skills = new Dictionary<SkillId, SkillProgress>(),
                Settings = new PlayerSettings("all", "22:00-09:00", false),
            };
            try
            {
                await players.CreatePlayerAsync(player, cancellationToken);
                telemetry.Track(player.Id, "player_created", new Dictionary<string, string> { ["completed"] = "false" });
            }
            catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                player = await players.GetPlayerAsync(externalId, cancellationToken)
                    ?? throw new InvalidOperationException("External identity was not found after a duplicate insert.");
            }
        }

        var subject = player.Id.ToString();
        var identity = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType);
        identity.SetClaim(Claims.Subject, subject);
        identity.SetDestinations(_ => []);

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(Scopes.OfflineAccess);
        principal.SetResources(options.Value.Audience);

        var applicationId = await applications.GetIdAsync(application, cancellationToken)
            ?? throw new InvalidOperationException("OpenIddict application ID is missing.");
        var authorization = await authorizations.CreateAsync(
            identity,
            subject,
            applicationId,
            AuthorizationTypes.Permanent,
            principal.GetScopes(),
            cancellationToken);
        principal.SetAuthorizationId(await authorizations.GetIdAsync(authorization, cancellationToken));
        return principal;
    }
}

using Kot.MongoDB.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using OpenIddict.Abstractions;
using Reki.Infrastructure.Auth;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Reki.Infrastructure.Mongo.Migrations;

internal sealed class AddYandexGrant(IOptions<AuthOptions> auth, IServiceScopeFactory scopeFactory) : RekiMigrationBase
{
    public override DatabaseVersion Version => new(2, 6, 5);

    public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var applications = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        var application = await applications.FindByClientIdAsync(auth.Value.ClientId, cancellationToken)
            ?? throw new InvalidOperationException("OpenIddict application is missing.");
        var descriptor = new OpenIddictApplicationDescriptor();
        await applications.PopulateAsync(descriptor, application, cancellationToken);
        descriptor.Permissions.Add(Permissions.Prefixes.GrantType + AuthConstants.Grants.Yandex);
        await applications.UpdateAsync(application, descriptor, cancellationToken);
    }
}

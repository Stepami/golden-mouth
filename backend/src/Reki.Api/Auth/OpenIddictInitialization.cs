using Microsoft.Extensions.Options;
using MongoDB.Driver;
using OpenIddict.Abstractions;
using OpenIddict.MongoDb;
using OpenIddict.MongoDb.Models;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Reki.Api.Auth;

internal static class OpenIddictInitialization
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var mongo = provider.GetRequiredService<IOpenIddictMongoDbContext>();
        var names = provider.GetRequiredService<IOptionsMonitor<OpenIddictMongoDbOptions>>().CurrentValue;
        var database = await mongo.GetDatabaseAsync(CancellationToken.None);

        await database.GetCollection<OpenIddictMongoDbApplication>(names.ApplicationsCollectionName)
            .Indexes.CreateOneAsync(new CreateIndexModel<OpenIddictMongoDbApplication>(
                Builders<OpenIddictMongoDbApplication>.IndexKeys.Ascending(item => item.ClientId),
                new CreateIndexOptions { Unique = true }));
        await database.GetCollection<OpenIddictMongoDbAuthorization>(names.AuthorizationsCollectionName)
            .Indexes.CreateOneAsync(new CreateIndexModel<OpenIddictMongoDbAuthorization>(
                Builders<OpenIddictMongoDbAuthorization>.IndexKeys
                    .Ascending(item => item.ApplicationId)
                    .Ascending(item => item.Scopes)
                    .Ascending(item => item.Status)
                    .Ascending(item => item.Subject)
                    .Ascending(item => item.Type)));
        await database.GetCollection<OpenIddictMongoDbToken>(names.TokensCollectionName)
            .Indexes.CreateManyAsync(
            [
                new CreateIndexModel<OpenIddictMongoDbToken>(
                    Builders<OpenIddictMongoDbToken>.IndexKeys.Ascending(item => item.ReferenceId),
                    new CreateIndexOptions<OpenIddictMongoDbToken>
                    {
                        PartialFilterExpression = Builders<OpenIddictMongoDbToken>.Filter.Exists(item => item.ReferenceId),
                        Unique = true,
                    }),
                new CreateIndexModel<OpenIddictMongoDbToken>(
                    Builders<OpenIddictMongoDbToken>.IndexKeys.Ascending(item => item.AuthorizationId),
                    new CreateIndexOptions<OpenIddictMongoDbToken>
                    {
                        PartialFilterExpression = Builders<OpenIddictMongoDbToken>.Filter.Exists(item => item.AuthorizationId),
                    }),
                new CreateIndexModel<OpenIddictMongoDbToken>(
                    Builders<OpenIddictMongoDbToken>.IndexKeys
                        .Ascending(item => item.ApplicationId)
                        .Ascending(item => item.Status)
                        .Ascending(item => item.Subject)
                        .Ascending(item => item.Type)),
            ]);

        var clientId = provider.GetRequiredService<IOptions<AuthOptions>>().Value.ClientId;
        var applications = provider.GetRequiredService<IOpenIddictApplicationManager>();
        if (await applications.FindByClientIdAsync(clientId) is null)
        {
            await applications.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = clientId,
                ClientType = ClientTypes.Public,
                Permissions =
                {
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.RefreshToken,
                    Permissions.Prefixes.GrantType + AuthConstants.MiniAppGrant,
                    Permissions.Prefixes.GrantType + AuthConstants.TelegramLoginGrant,
                },
            });
        }
    }
}

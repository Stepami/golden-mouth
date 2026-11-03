using Kot.MongoDB.Migrations;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using OpenIddict.Abstractions;
using OpenIddict.MongoDb;
using OpenIddict.MongoDb.Models;
using Reki.Infrastructure.Auth;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Reki.Infrastructure.Mongo.Migrations;

internal sealed class OpenIdDictInit(
    IOptions<AuthOptions> authOptions,
    IOptionsMonitor<OpenIddictMongoDbOptions> mongoOptions,
    IOpenIddictApplicationManager applications) : RekiMigrationBase
{
    public override DatabaseVersion Version => new(2, 5, 2);

    public override async Task UpAsync(
        IMongoDatabase db,
        IClientSessionHandle session,
        CancellationToken cancellationToken)
    {
        var names = mongoOptions.CurrentValue;
        var applicationIndex = new CreateIndexModel<OpenIddictMongoDbApplication>(
            Builders<OpenIddictMongoDbApplication>.IndexKeys.Ascending(item => item.ClientId),
            new CreateIndexOptions { Unique = true });
        await db.GetCollection<OpenIddictMongoDbApplication>(names.ApplicationsCollectionName)
            .Indexes.CreateOneAsync(applicationIndex, cancellationToken: cancellationToken);

        var authorizationIndex = new CreateIndexModel<OpenIddictMongoDbAuthorization>(
            Builders<OpenIddictMongoDbAuthorization>.IndexKeys
                .Ascending(item => item.ApplicationId)
                .Ascending(item => item.Scopes)
                .Ascending(item => item.Status)
                .Ascending(item => item.Subject)
                .Ascending(item => item.Type));
        await db.GetCollection<OpenIddictMongoDbAuthorization>(names.AuthorizationsCollectionName)
            .Indexes.CreateOneAsync(authorizationIndex, cancellationToken: cancellationToken);

        CreateIndexModel<OpenIddictMongoDbToken>[] tokenIndexes =
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
        ];
        await db.GetCollection<OpenIddictMongoDbToken>(names.TokensCollectionName)
            .Indexes.CreateManyAsync(tokenIndexes, cancellationToken);

        var clientId = authOptions.Value.ClientId;
        if (await applications.FindByClientIdAsync(clientId, cancellationToken) is null)
        {
            var descriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = clientId,
                ClientType = ClientTypes.Public,
                Permissions =
                {
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.RefreshToken,
                    Permissions.Prefixes.GrantType + "urn:reki:grant:telegram-mini-app",
                    Permissions.Prefixes.GrantType + "urn:reki:grant:telegram-login",
                },
            };
            await applications.CreateAsync(descriptor, cancellationToken);
        }
    }
}

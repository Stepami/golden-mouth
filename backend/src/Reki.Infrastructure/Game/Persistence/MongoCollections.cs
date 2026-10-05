using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Application.Game;
using Reki.Domain.Game;
using Reki.Infrastructure.Persistence;

namespace Reki.Infrastructure.Game;

internal static class MongoCollections
{
    public const string Players = "players";
    public const string Worlds = "worlds";
    public const string Posts = "posts";
    public const string Threads = "threads";
    public const string StarsMemory = "stars_memory";
    public const string Invites = "invites";
    public const string Challenges = "challenges";
    public const string Events = "events";
    public const string Analytics = "analytics";
}

using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Reki.Application.Posts;
using Reki.Application.Worlds;
using Reki.Domain.Worlds;
using Reki.Infrastructure.AI;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Interfaces;

namespace Reki.Infrastructure.Jobs;

internal sealed class RefreshEmbeddingsJob(
    IPostRepository posts,
    IWorldRepository worlds,
    IEmbeddingGenerator<string, Embedding<float>> generator,
    IOptions<EmbeddingOptions> options) : ITickerFunction
{
    private const string Revision = "mini-frida/32a8238/categorize-v1";

    public async Task ExecuteAsync(TickerFunctionContext context, CancellationToken cancellationToken = default)
    {
        if (!options.Value.Enabled)
        {
            return;
        }

        await foreach (var post in posts.ScanPostsAsync(Revision, cancellationToken))
        {
            var hash = TextHash(post.Text);
            if (post.Embedding is not null && post.EmbeddingRevision == Revision && post.EmbeddingTextHash == hash)
            {
                continue;
            }

            var vector = await EmbedAsync(post.Text, cancellationToken);
            await posts.TrySetEmbeddingAsync(post, vector, Revision, hash, cancellationToken);
        }

        await foreach (var world in worlds.ScanWorldsAsync(cancellationToken))
        {
            await RefreshWorldAsync(world, cancellationToken);
        }
    }

    private static string TextHash(string text) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

    private async Task RefreshWorldAsync(World world, CancellationToken cancellationToken)
    {
        var stars = new WorldStar[world.Stars.Count];
        var changed = false;
        for (var index = 0; index < stars.Length; index++)
        {
            var star = world.Stars[index];
            var recent = await posts.GetPostsAsync(world.Id, star.Id.ToAuthorId(), null, 5, cancellationToken);
            var profile = string.Join(
                '\n',
                new[] { star.Name, star.Role, star.Trait.ToString(), star.About, star.Voice }
                    .Concat(recent.Items.Select(post => post.Text)));
            var hash = TextHash(profile);
            if (star.Embedding is not null && star.EmbeddingRevision == Revision && star.EmbeddingTextHash == hash)
            {
                stars[index] = star;
                continue;
            }

            stars[index] = star with
            {
                Embedding = await EmbedAsync(profile, cancellationToken),
                EmbeddingRevision = Revision,
                EmbeddingTextHash = hash,
            };
            changed = true;
        }

        if (changed)
        {
            await worlds.TryReplaceWorldAsync(world with { Stars = stars, Version = world.Version + 1 }, world.Version, cancellationToken);
        }
    }

    private async Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken)
    {
        var input = text.StartsWith("categorize: ", StringComparison.Ordinal) ? text : $"categorize: {text}";
        var result = await generator.GenerateAsync([input], cancellationToken: cancellationToken);
        return result[0].Vector.ToArray();
    }
}

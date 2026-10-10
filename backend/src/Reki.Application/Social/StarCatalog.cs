using System.Text.Json;
using Reki.Application.Common.Exceptions;
using Reki.Domain.Social;
using Reki.Domain.Worlds;

namespace Reki.Application.Social;

public static class StarCatalog
{
    private static readonly IReadOnlyList<StarPersona> Personas = Load();

    public static IReadOnlyList<StarPersona> ForWorld(WorldId world)
    {
        return Personas.Where(persona => persona.World == world.Value).ToArray();
    }

    public static StarPersona Require(WorldId world, StarId star)
    {
        return Personas.SingleOrDefault(persona => persona.World == world.Value && persona.Id == star.Value)
            ?? throw new ResourceNotFoundException("The star does not belong to this world.");
    }

    public static bool Matches(WorldId world, StarId star, string name)
    {
        return Personas.Any(persona => persona.World == world.Value &&
            persona.Id == star.Value && persona.Name == name);
    }

    private static IReadOnlyList<StarPersona> Load()
    {
        using var stream = typeof(StarCatalog).Assembly.GetManifestResourceStream("Reki.StarPersonas.json")
                           ?? throw new InvalidOperationException("The star catalog is missing.");
        StarPersona[] personas = JsonSerializer.Deserialize(stream, StarCatalogJsonContext.Default.StarPersonaArray)
            ?? throw new InvalidOperationException("The star catalog is empty.");
        if (personas.Length == 0 || personas.Select(persona => persona.Id).Distinct(StringComparer.Ordinal).Count() != personas.Length)
        {
            throw new InvalidOperationException("The star catalog contains invalid identities.");
        }

        return Array.AsReadOnly(personas);
    }
}

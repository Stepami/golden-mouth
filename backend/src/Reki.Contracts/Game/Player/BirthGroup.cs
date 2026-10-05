using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Reki.Contracts.Game;

public enum BirthGroup
{
    [System.Text.Json.Serialization.JsonStringEnumMemberName("14-17")]
    Teen,

    [System.Text.Json.Serialization.JsonStringEnumMemberName("18+")]
    Adult,
}


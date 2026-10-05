using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Reki.Contracts.Game;

public enum GameEventKind
{
    Collab,
    Hate,
    Gossip,
    Brand,
}

